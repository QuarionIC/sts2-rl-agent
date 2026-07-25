"""Multi-seed CONFIRMATION phase for the from-scratch ablation screen.

Why this exists
---------------
``scripts/ablation_study.py`` runs each arm ONCE. The same baseline config
produced 5.445 and 6.68 mean floors at its 1M eval across two runs (~1.2
floors of seed/init variance), and because the least-squares slope over three
equally spaced evals reduces to ``(y_last - y_first) / span``, that implies
slope noise of roughly +/-0.35 per million steps. Every slope gap the screen
found is INSIDE that band, so the screen is a coarse ranking, not a verdict
(see docs/ABLATION_STUDY.md).

This harness turns the ranking into a decision:

* each named config runs on ``--seeds`` independent seeds (default 0,1,2),
  using the trainer's ``--seed`` (python/numpy/torch + model + TRAINING env
  seeds; eval keeps its fixed held-out seed block, so all seeds are scored on
  identical eval runs);
* 3M steps with evals every 500k = **6 eval points** per run instead of 3,
  which roughly halves the slope-estimate noise;
* per-seed slope and final floors are aggregated to mean +/- standard error,
  with a Welch t-test vs the A0 baseline;
* a LEVEL GATE runs before the slope test (a config finishing more than the
  pooled SE below baseline is disqualified whatever its slope -- screen arm
  A2 had the best slope in the study while finishing 2.31 floors behind,
  merely climbing out of a hole);
* a config then wins ONLY if its mean slope beats the baseline's by more than
  the pooled standard error. Otherwise the verdict is "not distinguishable"
  and the simpler/cheaper config is recommended. With n=3 the power is low
  and the report says so -- the point is to stop over-reading single runs,
  not to manufacture a winner.

Runs are strictly SEQUENTIAL (one training process at a time; 16 GB box) and
reuse the ablation harness's hardening: BLAS caps, commit-headroom preflight,
unconditional ``taskkill /T`` process-tree reap, crash tolerance with one
retry for zero-eval crashes, and incremental results in
``output/confirm/results.json``.

There is deliberately NO early-stop rule here (the screen has one): cutting a
seed short would bias its slope and destroy the very comparison this phase
exists to make.

Usage
-----
    python scripts/confirm_phase.py                       # default configs
    python scripts/confirm_phase.py --configs A0,A1,A4    # explicit
    python scripts/confirm_phase.py --configs A0,FINAL    # FINAL = combined
    python scripts/confirm_phase.py --seeds 5             # seeds 0..4
    python scripts/confirm_phase.py --dry-run             # print the plan
    python scripts/confirm_phase.py --analyze-only        # stats from results
    python scripts/confirm_phase.py --analyze-only --write-docs

Default configs = the A0 baseline + the top-2 completed screen arms by floor
slope, read from ``output/ablation/results.json``.
"""

from __future__ import annotations

import os

# Same BLAS caps as the trainer (children inherit them even before the
# trainer's own setdefault runs).
for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import math
import shutil
import subprocess
import sys
import time
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parent
if str(_SCRIPTS) not in sys.path:            # importable as a module in tests
    sys.path.insert(0, str(_SCRIPTS))

from ablation_study import (  # noqa: E402  (path bootstrap must precede)
    ARM_ORDER,
    ARMS,
    RESULTS_PATH as SCREEN_RESULTS_PATH,
    act2_reaches,
    commit_available_mb,
    kill_tree,
    preflight_commit,
    read_evals,
)

REPO = _SCRIPTS.parent
TRAINER = _SCRIPTS / "train_necrobinder.py"
CONFIRM_DIR = REPO / "output" / "confirm"
RESULTS_PATH = CONFIRM_DIR / "results.json"
DOC_PATH = REPO / "docs" / "ABLATION_STUDY.md"
DOC_START = "<!-- confirm-phase:results:start -->"
DOC_END = "<!-- confirm-phase:results:end -->"

RUN_STEPS = 3_000_000
EVAL_FREQ = 500_000          # 6 eval points per run (1 x 500k .. 6 x 3M)
EVAL_EPISODES = 200
N_ENVS = 16
DEFAULT_SEEDS = 3
N_TOP_ARMS = 2               # screened arms carried into confirmation
RUN_TIMEOUT_S = 5 * 3600     # 3M steps + 6 x 200-episode evals, generous
POLL_S = 30


# ---------------------------------------------------------------------------
# Config resolution
# ---------------------------------------------------------------------------

def load_screen() -> dict:
    """The 8-arm screen's results.json (``{}`` if it does not exist yet)."""
    if SCREEN_RESULTS_PATH.exists():
        return json.loads(SCREEN_RESULTS_PATH.read_text(encoding="utf-8"))
    return {}


def final_flags(screen: dict) -> list[str]:
    """Flags of the FINAL combined config from the screen's selection.

    Empty list = the combination is the pure baseline (no factor won), which
    is a legitimate outcome, not a missing value.
    """
    sel = screen.get("selection") or {}
    if "final_flags" in sel:
        return list(sel["final_flags"])
    rec = (screen.get("arms") or {}).get("FINAL")
    if rec is not None:
        return list(rec.get("flags") or [])
    raise SystemExit(
        "FINAL requested but the screen has no selection/FINAL record yet "
        f"({SCREEN_RESULTS_PATH}); run ablation_study.py --select-only first "
        "or name the arms explicitly with --configs"
    )


def passes_screen_level_gate(rec: dict, baseline: dict | None) -> bool:
    """The screen's LEVEL GATE, applied before any slope ranking.

    A high slope from a much worse starting point is not evidence of a better
    config: screen arm A2 (mean-pool) had the best slope in the whole screen
    (+0.55/M) while finishing 2.31 floors BEHIND the baseline -- it was
    climbing out of a hole. Carrying such an arm into the (expensive)
    confirmation phase would repeat the mistake ablation_study.select_winners
    now guards against, so the same NOISE_FLOORS band gates it here.
    """
    if baseline is None or rec.get("final_floors") is None:
        return True
    b_final = baseline.get("final_floors")
    if b_final is None:
        return True
    return rec["final_floors"] - b_final >= -ab_noise_floors()


def ab_noise_floors() -> float:
    """The screen's measured single-seed noise band (imported live so the two
    harnesses can never drift apart)."""
    import ablation_study

    return float(ablation_study.NOISE_FLOORS)


def top_screened_arms(screen: dict, k: int = N_TOP_ARMS) -> list[str]:
    """The k completed non-baseline arms with the highest floor slope, after
    the level gate."""
    arms = screen.get("arms") or {}
    baseline = arms.get("A0")
    ranked = [
        (rec["floor_slope_per_m"], name)
        for name, rec in arms.items()
        if name in ARM_ORDER[1:]
        and rec.get("status") == "completed"
        and rec.get("floor_slope_per_m") is not None
        and passes_screen_level_gate(rec, baseline)
    ]
    ranked.sort(key=lambda t: (-t[0], ARM_ORDER.index(t[1])))
    return [name for _, name in ranked[:k]]


def resolve_configs(names: str | None, screen: dict) -> list[tuple[str, list[str]]]:
    """``[(config name, trainer flags)]``, baseline A0 always first."""
    if names:
        wanted = [n.strip() for n in names.split(",") if n.strip()]
    else:
        top = top_screened_arms(screen)
        if len(top) < N_TOP_ARMS:
            raise SystemExit(
                f"only {len(top)} completed screen arm(s) with a slope in "
                f"{SCREEN_RESULTS_PATH}; the screen must finish before the "
                f"default top-{N_TOP_ARMS} selection works. Pass --configs "
                f"explicitly to override."
            )
        wanted = ["A0", *top]
    if "A0" not in wanted:
        wanted = ["A0", *wanted]          # baseline is the comparison anchor
    out: list[tuple[str, list[str]]] = []
    for name in dict.fromkeys(wanted):    # de-dupe, keep order
        if name == "FINAL":
            out.append((name, final_flags(screen)))
        elif name in ARMS:
            out.append((name, list(ARMS[name][1])))
        else:
            raise SystemExit(
                f"unknown config {name!r}; choose from {[*ARM_ORDER, 'FINAL']}")
    return out


def describe(name: str, flags: list[str]) -> str:
    if name in ARMS:
        return ARMS[name][0]
    return f"FINAL combined config ({' '.join(flags) or 'pure baseline'})"


# ---------------------------------------------------------------------------
# Results persistence
# ---------------------------------------------------------------------------

def run_key(config: str, seed: int) -> str:
    return f"{config}_s{seed}"


def load_results() -> dict:
    if RESULTS_PATH.exists():
        return json.loads(RESULTS_PATH.read_text(encoding="utf-8"))
    return {
        "config": {
            "run_steps": RUN_STEPS, "eval_freq": EVAL_FREQ,
            "eval_episodes": EVAL_EPISODES, "n_envs": N_ENVS,
            "early_stop": False,
        },
        "runs": {},
    }


def save_results(results: dict) -> None:
    CONFIRM_DIR.mkdir(parents=True, exist_ok=True)
    results["updated"] = time.strftime("%Y-%m-%d %H:%M:%S")
    tmp = RESULTS_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(results, indent=2), encoding="utf-8")
    tmp.replace(RESULTS_PATH)


# ---------------------------------------------------------------------------
# Statistics
# ---------------------------------------------------------------------------

def lsq_slope(xs: list[float], ys: list[float]) -> float | None:
    """Least-squares slope of ys vs xs (None if undetermined)."""
    n = len(xs)
    if n < 2 or n != len(ys):
        return None
    mx = sum(xs) / n
    my = sum(ys) / n
    denom = sum((x - mx) ** 2 for x in xs)
    if denom == 0:
        return None
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom


def eval_slope(evals: list[dict]) -> float | None:
    """Floor slope per million steps over a run's eval points."""
    return lsq_slope([ev["steps"] / 1e6 for ev in evals],
                     [ev["mean_floors"] for ev in evals])


def mean_se(values: list[float]) -> dict:
    """mean, sample sd (ddof=1), standard error of the mean, n."""
    n = len(values)
    if n == 0:
        return {"n": 0, "mean": None, "sd": None, "se": None}
    mean = sum(values) / n
    if n == 1:
        return {"n": 1, "mean": mean, "sd": None, "se": None}
    var = sum((v - mean) ** 2 for v in values) / (n - 1)
    sd = math.sqrt(var)
    return {"n": n, "mean": mean, "sd": sd, "se": sd / math.sqrt(n)}


def _betacf(a: float, b: float, x: float) -> float:
    """Continued fraction for the incomplete beta function (Lentz)."""
    tiny, eps, max_iter = 1e-30, 3e-16, 300
    qab, qap, qam = a + b, a + 1.0, a - 1.0
    c = 1.0
    d = 1.0 - qab * x / qap
    if abs(d) < tiny:
        d = tiny
    d = 1.0 / d
    h = d
    for m in range(1, max_iter + 1):
        m2 = 2 * m
        aa = m * (b - m) * x / ((qam + m2) * (a + m2))
        d = 1.0 + aa * d
        if abs(d) < tiny:
            d = tiny
        c = 1.0 + aa / c
        if abs(c) < tiny:
            c = tiny
        d = 1.0 / d
        h *= d * c
        aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2))
        d = 1.0 + aa * d
        if abs(d) < tiny:
            d = tiny
        c = 1.0 + aa / c
        if abs(c) < tiny:
            c = tiny
        d = 1.0 / d
        delta = d * c
        h *= delta
        if abs(delta - 1.0) < eps:
            break
    return h


def betainc_reg(a: float, b: float, x: float) -> float:
    """Regularised incomplete beta I_x(a, b). No scipy in this venv."""
    if x <= 0.0:
        return 0.0
    if x >= 1.0:
        return 1.0
    front = math.exp(
        math.lgamma(a + b) - math.lgamma(a) - math.lgamma(b)
        + a * math.log(x) + b * math.log1p(-x)
    )
    if x < (a + 1.0) / (a + b + 2.0):
        return front * _betacf(a, b, x) / a
    return 1.0 - math.exp(
        math.lgamma(a + b) - math.lgamma(a) - math.lgamma(b)
        + b * math.log1p(-x) + a * math.log(x)
    ) * _betacf(b, a, 1.0 - x) / b


def t_sf(t: float, df: float) -> float:
    """P(T > t) for Student's t with df degrees of freedom."""
    if df <= 0:
        return float("nan")
    x = df / (df + t * t)
    tail = 0.5 * betainc_reg(df / 2.0, 0.5, x)      # P(T > |t|)
    return tail if t > 0 else 1.0 - tail


def welch_ttest(a: list[float], b: list[float]) -> dict | None:
    """Welch (unequal-variance) two-sample t-test, two-sided.

    Returns None when it is undefined (fewer than 2 samples per group, or
    both groups have zero variance -- with n=3 that happens and reporting a
    fabricated p-value would be worse than reporting nothing).
    """
    if len(a) < 2 or len(b) < 2:
        return None
    sa, sb = mean_se(a), mean_se(b)
    va = sa["sd"] ** 2 / sa["n"]
    vb = sb["sd"] ** 2 / sb["n"]
    if va + vb == 0:
        return None
    t = (sa["mean"] - sb["mean"]) / math.sqrt(va + vb)
    df = (va + vb) ** 2 / (
        va ** 2 / (sa["n"] - 1) + vb ** 2 / (sb["n"] - 1)
    )
    return {"t": t, "df": df, "p_two_sided": 2.0 * t_sf(abs(t), df)}


def pooled_se(a: dict, b: dict) -> float | None:
    """SE of the difference of two independent means."""
    if a.get("se") is None or b.get("se") is None:
        return None
    return math.sqrt(a["se"] ** 2 + b["se"] ** 2)


def decide(config: str, cfg: dict, base: dict) -> tuple[bool, str]:
    """Decision rule, in order:

    1. LEVEL GATE -- a config whose mean FINAL floors sits more than the
       pooled SE below the baseline's is disqualified outright, whatever its
       slope. (Screen arm A2 had the best slope in the study, +0.55/M, while
       finishing 2.31 floors behind baseline: it was climbing out of a hole,
       not learning better. Slope-first would have adopted it.)
    2. A config wins ONLY if its mean slope beats the baseline's by more than
       the pooled standard error of the two means.

    Returns (is_winner, verdict line).
    """
    if cfg["slope"]["mean"] is None or base["slope"]["mean"] is None:
        return False, f"{config}: no usable slope -- keep baseline"
    lvl_delta = None
    if cfg["final"]["mean"] is not None and base["final"]["mean"] is not None:
        lvl_delta = cfg["final"]["mean"] - base["final"]["mean"]
        lvl_se = pooled_se(cfg["final"], base["final"])
        if lvl_se is not None and lvl_delta < -lvl_se:
            return False, (
                f"{config}: DISQUALIFIED on level (final floors "
                f"{lvl_delta:+.2f} vs baseline, beyond the pooled SE "
                f"{lvl_se:.2f}) regardless of slope")
    delta = cfg["slope"]["mean"] - base["slope"]["mean"]
    se_c, se_b = cfg["slope"]["se"], base["slope"]["se"]
    if se_c is None or se_b is None:
        return False, (
            f"{config}: slope delta {delta:+.3f}/M but only "
            f"{cfg['slope']['n']} vs {base['slope']['n']} seed(s) -- no "
            f"standard error, NOT DISTINGUISHABLE (need >=2 seeds per config)")
    pooled = pooled_se(cfg["slope"], base["slope"])
    if delta > pooled:
        return True, (
            f"{config}: WIN on slope ({delta:+.3f}/M > pooled SE "
            f"{pooled:.3f}/M)")
    return False, (
        f"{config}: NOT DISTINGUISHABLE from baseline (slope {delta:+.3f}/M "
        f"vs pooled SE {pooled:.3f}/M) -- prefer the simpler config")


def analyze(results: dict) -> dict:
    """Per-config aggregation, verdicts and the recommendation."""
    per_config: dict[str, dict] = {}
    for rec in results.get("runs", {}).values():
        cfg = per_config.setdefault(rec["config"], {
            "config": rec["config"], "flags": rec.get("flags", []),
            "seeds": [], "slopes": [], "finals": [], "act2": [],
            "wins": [], "statuses": {},
        })
        cfg["statuses"][rec["seed"]] = rec["status"]
        if rec["status"] != "completed" or rec.get("slope_per_m") is None:
            continue
        cfg["seeds"].append(rec["seed"])
        cfg["slopes"].append(rec["slope_per_m"])
        cfg["finals"].append(rec["final_floors"])
        cfg["act2"].append(rec["final_act2"])
        cfg["wins"].append(rec["final_win_rate"])
    for cfg in per_config.values():
        order = sorted(range(len(cfg["seeds"])), key=lambda i: cfg["seeds"][i])
        for key in ("seeds", "slopes", "finals", "act2", "wins"):
            cfg[key] = [cfg[key][i] for i in order]
        cfg["slope"] = mean_se(cfg["slopes"])
        cfg["final"] = mean_se([f for f in cfg["finals"] if f is not None])

    base = per_config.get("A0")
    verdicts: list[str] = []
    winners: list[str] = []
    tests: dict[str, dict | None] = {}
    if base is None:
        verdicts.append("A0 baseline has no completed run -- no comparison "
                        "possible")
    else:
        for name, cfg in per_config.items():
            if name == "A0":
                continue
            tests[name] = welch_ttest(cfg["slopes"], base["slopes"])
            won, line = decide(name, cfg, base)
            tv = tests[name]
            if tv is not None:
                line += (f"; Welch t={tv['t']:+.2f}, df={tv['df']:.1f}, "
                         f"p={tv['p_two_sided']:.3f} (descriptive only)")
            verdicts.append(line)
            if won:
                winners.append(name)
    if winners:
        # Among winners, the cheapest (fewest extra flags) that wins.
        recommended = min(
            winners,
            key=lambda n: (len(per_config[n]["flags"]),
                           -(per_config[n]["slope"]["mean"] or 0.0)),
        )
        reason = "beat the baseline by more than the pooled SE"
    else:
        recommended = "A0"
        reason = ("nothing beat the baseline by more than the pooled SE; "
                  "recommend the simplest/cheapest config")
    return {
        "per_config": per_config, "verdicts": verdicts, "winners": winners,
        "tests": tests, "recommended": recommended, "reason": reason,
    }


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------

def fmt(v, spec: str = ".2f") -> str:
    return "-" if v is None else format(v, spec)


def fmt_pm(stat: dict, spec: str = ".3f", pm: str = "+/-") -> str:
    """mean +/- SE, with the separator chosen by the sink (the Windows
    console encoding is not guaranteed to carry U+00B1; markdown is UTF-8)."""
    if stat.get("mean") is None:
        return "-"
    if stat.get("se") is None:
        return f"{stat['mean']:{spec}} (n=1)"
    return f"{stat['mean']:{spec}} {pm} {stat['se']:{spec}}"


def print_report(results: dict, analysis: dict) -> None:
    print("\n=== confirmation phase ===")
    print(f"{'config':<8}{'seeds':<8}{'slope/M (mean +/- SE)':<26}"
          f"{'final floors (mean +/- SE)':<28}per-seed slopes")
    for name, cfg in sorted(analysis["per_config"].items()):
        per_seed = ", ".join(f"s{s}:{v:+.3f}"
                             for s, v in zip(cfg["seeds"], cfg["slopes"]))
        print(f"{name:<8}{len(cfg['seeds']):<8}{fmt_pm(cfg['slope']):<26}"
              f"{fmt_pm(cfg['final'], '.2f'):<28}{per_seed or '-'}")
    print()
    for line in analysis["verdicts"]:
        print(line)
    print(f"\nwinners: {analysis['winners'] or 'none'}")
    print(f"recommended: {analysis['recommended']} ({analysis['reason']})")
    print("n=3 seeds per config gives LOW statistical power: the mean +/- SE "
          "and the raw per-seed values above are the primary evidence; the "
          "p-values are descriptive, not a significance gate.", flush=True)


def render_markdown(results: dict, analysis: dict) -> str:
    """The results block written between the doc markers."""
    lines = [
        f"_Generated by `scripts/confirm_phase.py --analyze-only "
        f"--write-docs` from `output/confirm/results.json` "
        f"(updated {results.get('updated', 'n/a')})._",
        "",
        "| config | seeds | per-seed slope/M | mean slope/M +/- SE | "
        "per-seed final floors | mean final +/- SE | Welch t vs A0 |",
        "|--------|-------|------------------|-------------------|"
        "-----------------------|-----------------|---------------|",
    ]
    for name, cfg in sorted(analysis["per_config"].items()):
        tv = analysis["tests"].get(name)
        tcell = ("-" if tv is None else
                 f"t={tv['t']:+.2f}, df={tv['df']:.1f}, p={tv['p_two_sided']:.3f}")
        lines.append(
            f"| {name} | {len(cfg['seeds'])} | "
            f"{', '.join(f'{v:+.3f}' for v in cfg['slopes']) or '-'} | "
            f"{fmt_pm(cfg['slope'])} | "
            f"{', '.join(f'{v:.2f}' for v in cfg['finals']) or '-'} | "
            f"{fmt_pm(cfg['final'], '.2f')} | {tcell} |"
        )
    lines += ["", "Verdicts (rule: win only if the mean slope beats the "
              "baseline's by more than the pooled SE):", ""]
    lines += [f"- {v}" for v in analysis["verdicts"]] or ["- (none yet)"]
    lines += [
        "",
        f"**Recommended config: {analysis['recommended']}** — "
        f"{analysis['reason']}.",
        "",
        "n=3 seeds per config is LOW POWER. The mean +/- SE and the raw "
        "per-seed values are the evidence; the Welch p-values are reported "
        "descriptively and are not used as a significance gate.",
    ]
    return "\n".join(lines)


def write_docs(results: dict, analysis: dict) -> None:
    text = DOC_PATH.read_text(encoding="utf-8")
    block = render_markdown(results, analysis)
    if DOC_START in text and DOC_END in text:
        head, rest = text.split(DOC_START, 1)
        _, tail = rest.split(DOC_END, 1)
        text = f"{head}{DOC_START}\n\n{block}\n\n{DOC_END}{tail}"
    else:                                   # markers gone: append, never cut
        text = (f"{text.rstrip()}\n\n## Confirmation phase results\n\n"
                f"{DOC_START}\n\n{block}\n\n{DOC_END}\n")
    DOC_PATH.write_text(text, encoding="utf-8")
    print(f"[docs] updated {DOC_PATH}")


# ---------------------------------------------------------------------------
# Run execution
# ---------------------------------------------------------------------------

def build_cmd(config: str, flags: list[str], seed: int, out_dir: Path) -> list[str]:
    """Exact trainer command line for one confirmation run."""
    return [
        sys.executable, str(TRAINER),
        "--stage", "G1",
        "--total-steps", str(RUN_STEPS),
        "--n-envs", str(N_ENVS),
        "--eval-freq", str(EVAL_FREQ),
        "--eval-episodes", str(EVAL_EPISODES),
        "--checkpoint-freq", str(RUN_STEPS),
        "--seed", str(seed),
        "--output-dir", str(out_dir),
        *flags,
    ]


def summarize(config: str, flags: list[str], seed: int, evals: list[dict],
              status: str, wall_s: float) -> dict:
    last = evals[-1] if evals else None
    return {
        "config": config,
        "factor": describe(config, flags),
        "seed": seed,
        "flags": flags,
        "status": status,
        "wall_s": round(wall_s, 1),
        "evals": evals,
        "n_evals": len(evals),
        "slope_per_m": eval_slope(evals),
        "final_floors": last["mean_floors"] if last else None,
        "final_act2": act2_reaches(last) if last else None,
        "final_win_rate": last["win_rate"] if last else None,
    }


def run_one(config: str, flags: list[str], seed: int, results: dict,
            retries: int = 1) -> dict:
    key = run_key(config, seed)
    out_dir = CONFIRM_DIR / key
    if out_dir.exists():
        shutil.rmtree(out_dir)      # stale partial output corrupts parsing
    out_dir.mkdir(parents=True, exist_ok=True)
    log_path = out_dir / "train.log"

    cmd = build_cmd(config, flags, seed, out_dir)
    print(f"\n[{key}] {describe(config, flags)}")
    print(f"[{key}] launching: {' '.join(cmd)}", flush=True)
    commit_at_start = preflight_commit(key)

    start = time.time()
    status = "completed"
    with open(log_path, "w", encoding="utf-8") as log_f:
        proc = subprocess.Popen(
            cmd, stdout=log_f, stderr=subprocess.STDOUT,
            cwd=str(REPO), env=os.environ.copy(),
        )
        try:
            reported = 0
            while True:
                try:
                    proc.wait(timeout=POLL_S)
                    break
                except subprocess.TimeoutExpired:
                    pass
                evals = read_evals(out_dir)
                if len(evals) > reported:
                    for ev in evals[reported:]:
                        print(f"[{key}] eval @ {ev['steps']:,}: "
                              f"floors={ev['mean_floors']:.2f} "
                              f"act2={act2_reaches(ev)} "
                              f"win={ev['win_rate']:.1%}", flush=True)
                    reported = len(evals)
                # NO early stop: truncating a seed would bias its slope.
                if time.time() - start > RUN_TIMEOUT_S:
                    print(f"[{key}] TIMEOUT after {RUN_TIMEOUT_S/3600:.1f}h; "
                          f"killing", flush=True)
                    kill_tree(proc.pid)
                    proc.wait(timeout=60)
                    status = "timeout"
                    break
        except BaseException:
            kill_tree(proc.pid)     # never leave a half-dead run behind
            raise
        finally:
            # ALWAYS reap the tree: a crashed trainer strands its 16 env
            # workers, whose resident commit then OOMs every later run.
            kill_tree(proc.pid)
    if status == "completed" and proc.returncode != 0:
        status = f"crashed (rc={proc.returncode})"

    wall = time.time() - start
    record = summarize(config, flags, seed, read_evals(out_dir), status, wall)
    record["commit_available_mb_at_start"] = commit_at_start
    if status.startswith("crashed") and not record["evals"] and retries > 0:
        print(f"[{key}] zero-eval crash; retrying ({retries} left)", flush=True)
        return run_one(config, flags, seed, results, retries=retries - 1)
    results["runs"][key] = record
    save_results(results)
    print(f"[{key}] {status} in {wall/60:.1f} min; "
          f"evals={record['n_evals']} slope={fmt(record['slope_per_m'], '+.3f')} "
          f"final={fmt(record['final_floors'])}", flush=True)
    return record


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    parser.add_argument("--configs", type=str, default=None,
                        help="Comma-separated configs, e.g. 'A0,A1,A4' or "
                             "'A0,FINAL'. Default: A0 + the top-2 completed "
                             "screen arms by slope. A0 is always included.")
    parser.add_argument("--seeds", type=int, default=DEFAULT_SEEDS,
                        help=f"Number of seeds per config, 0..n-1 "
                             f"(default {DEFAULT_SEEDS} -> seeds 0,1,2).")
    parser.add_argument("--seed-list", type=str, default=None,
                        help="Explicit comma-separated seeds (overrides "
                             "--seeds), e.g. '0,1,2,7'.")
    parser.add_argument("--force", action="store_true",
                        help="Re-run runs already recorded in results.json.")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print the plan (exact commands) and exit.")
    parser.add_argument("--analyze-only", action="store_true",
                        help="Recompute statistics from results.json and exit.")
    parser.add_argument("--write-docs", action="store_true",
                        help="Write the analysis into docs/ABLATION_STUDY.md "
                             "between the confirm-phase markers.")
    args = parser.parse_args()

    results = load_results()

    if args.analyze_only:
        analysis = analyze(results)
        print_report(results, analysis)
        results["analysis"] = {
            "verdicts": analysis["verdicts"], "winners": analysis["winners"],
            "recommended": analysis["recommended"], "reason": analysis["reason"],
            "per_config": {
                name: {"seeds": cfg["seeds"], "slopes": cfg["slopes"],
                       "finals": cfg["finals"], "slope": cfg["slope"],
                       "final": cfg["final"]}
                for name, cfg in analysis["per_config"].items()
            },
            "tests": analysis["tests"],
        }
        save_results(results)
        if args.write_docs:
            write_docs(results, analysis)
        return

    screen = load_screen()
    configs = resolve_configs(args.configs, screen)
    if args.seed_list:
        seeds = [int(s) for s in args.seed_list.split(",") if s.strip()]
    else:
        seeds = list(range(args.seeds))

    plan = [(name, flags, seed) for name, flags in configs for seed in seeds]
    print(f"confirmation phase: {len(configs)} configs x {len(seeds)} seeds "
          f"= {len(plan)} runs of {RUN_STEPS:,} steps "
          f"(evals every {EVAL_FREQ:,}, {EVAL_EPISODES} episodes), sequential")
    for name, flags, seed in plan:
        print(f"  {run_key(name, seed):<10} {' '.join(flags) or '(baseline)'}")
    if args.dry_run:
        print("\n--dry-run: commands that would run\n")
        for name, flags, seed in plan:
            print(" ".join(build_cmd(name, flags, seed,
                                     CONFIRM_DIR / run_key(name, seed))))
        return

    print(f"commit available now: {commit_available_mb()} MB", flush=True)
    study_start = time.time()
    for name, flags, seed in plan:
        key = run_key(name, seed)
        prior = results["runs"].get(key)
        if prior is not None and not args.force:
            print(f"[{key}] already recorded ({prior['status']}); skipping "
                  f"(--force to redo)", flush=True)
            continue
        try:
            run_one(name, flags, seed, results)
        except Exception as exc:            # one bad run must not sink the phase
            results["runs"][key] = summarize(name, flags, seed, [],
                                             f"harness error: {exc}", 0.0)
            save_results(results)
            print(f"[{key}] harness error: {exc}", flush=True)
        print_report(results, analyze(results))

    analysis = analyze(results)
    print_report(results, analysis)
    results["analysis"] = {
        "verdicts": analysis["verdicts"], "winners": analysis["winners"],
        "recommended": analysis["recommended"], "reason": analysis["reason"],
    }
    save_results(results)
    if args.write_docs:
        write_docs(results, analysis)
    print(f"confirmation phase done in "
          f"{(time.time() - study_start)/3600:.2f} h", flush=True)


if __name__ == "__main__":
    main()
