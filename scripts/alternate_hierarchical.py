#!/usr/bin/env python3
"""Alternating refinement of the combat agent and the run agent.

Training them once each is not enough, and the reason is a distribution shift
that only appears after the split works. The combat agent is bootstrapped on
:class:`ProgressiveDeckSampler` -- a synthetic spread of plausible mid-run
decks. The run agent then learns to build decks of its own. The moment it
succeeds, the decks reaching combat stop looking like the sampler's guesses,
and the combat agent is being asked to play a distribution it was never
trained on. Symmetrically, a stronger combat agent survives fights the run
agent previously lost, which changes which map states the run agent reaches.

Each round therefore:

1. Harvests the decks the CURRENT run agent actually produces, and refits the
   combat agent on those (round 1 falls back to the synthetic sampler, since
   there is no run agent yet).
2. Retrains the run agent against the refreshed, frozen combat agent.

Both halves are evaluated every round so the alternation can be stopped when
it stops paying, rather than run for a fixed count on faith.

Example
-------
    python scripts/alternate_hierarchical.py --rounds 3 \
        --combat-steps 3000000 --run-steps 3000000
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import pickle
import subprocess
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PY = str(REPO / ".venv" / "Scripts" / "python.exe")
if not Path(PY).exists():
    PY = sys.executable


def harvest_decks(run_model: str, combat_model: str, out_path: Path,
                  episodes: int, ascension: int, max_act_count: int) -> int:
    """Record (deck, relics, potions, hp_fraction) at every combat the run
    agent enters, for use as the combat agent's next training distribution."""
    script = f'''
import os
for v in ("OPENBLAS_NUM_THREADS","OMP_NUM_THREADS","MKL_NUM_THREADS"):
    os.environ.setdefault(v,"1")
import pickle, numpy as np
import sts2_env.events
from sb3_contrib import MaskablePPO
from sts2_env.gym_env.hierarchical_run_env import HierarchicalRunEnv, PolicyCombatController
from sts2_env.run.run_manager import RunManager

combat = MaskablePPO.load({combat_model!r}, device="cpu")
run = MaskablePPO.load({run_model!r}, device="cpu")
env = HierarchicalRunEnv(character_id="Necrobinder", ascension_level={ascension},
                         max_act_count={max_act_count},
                         combat_controller=PolicyCombatController(combat, deterministic=False))
env.set_shaping_scale(0.0)

samples = []
_orig = env._combat_action
def spy(mask):
    c = env._mgr.get_combat_state()
    if c is not None and not c.is_over and getattr(c, "_snap_taken", False) is False:
        try:
            c._snap_taken = True
        except Exception:
            pass
        rs = env._mgr.run_state
        p = rs.player
        samples.append((
            [(cd.card_id, cd.upgraded) for cd in p.deck],
            [str(r) for r in rs.relics],
            [str(x) for x in (p.potions or []) if x is not None],
            float(p.current_hp) / float(max(p.max_hp, 1)),
        ))
    return _orig(mask)
env._combat_action = spy

for i in range({episodes}):
    obs, info = env.reset(seed=80_000_000 + i)
    done = tr = False
    n = 0
    while not (done or tr) and n < 2000:
        m = env.action_masks()
        a, _ = run.predict(obs, action_masks=m, deterministic=False)
        obs, r, done, tr, info = env.step(int(a))
        n += 1

with open({str(out_path)!r}, "wb") as fh:
    pickle.dump(samples, fh)
print(f"harvested {{len(samples)}} combat-entry decks")
'''
    r = subprocess.run([PY, "-c", script], cwd=REPO, capture_output=True, text=True)
    sys.stdout.write(r.stdout)
    if r.returncode != 0:
        sys.stderr.write(r.stderr[-4000:])
        return 0
    try:
        with out_path.open("rb") as fh:
            return len(pickle.load(fh))
    except Exception:
        return 0


def launch(cmd: list[str], log: Path) -> int:
    print(f"  $ {' '.join(cmd)}", flush=True)
    with log.open("w", encoding="utf-8") as fh:
        p = subprocess.run(cmd, cwd=REPO, stdout=fh, stderr=subprocess.STDOUT)
    return p.returncode


def last_eval(path: Path) -> dict:
    try:
        lines = [l for l in path.read_text(encoding="utf-8").splitlines() if l.strip()]
        return json.loads(lines[-1]) if lines else {}
    except Exception:
        return {}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--rounds", type=int, default=3)
    ap.add_argument("--combat-steps", type=int, default=3_000_000)
    ap.add_argument("--run-steps", type=int, default=3_000_000)
    ap.add_argument("--n-envs", type=int, default=16)
    ap.add_argument("--eval-freq", type=int, default=500_000)
    ap.add_argument("--eval-episodes", type=int, default=200)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--harvest-episodes", type=int, default=150)
    ap.add_argument("--seed-combat", default=None,
                    help="Existing combat model to start round 1 from "
                         "(skips the bootstrap combat training)")
    ap.add_argument("--out-root", default="output/hier_alt")
    ap.add_argument("--device", default="cuda")
    args = ap.parse_args()

    root = REPO / args.out_root
    root.mkdir(parents=True, exist_ok=True)
    ledger = root / "rounds.jsonl"

    combat_model = args.seed_combat
    run_model = None

    for rnd in range(1, args.rounds + 1):
        t0 = time.time()
        print(f"\n{'='*70}\nROUND {rnd}/{args.rounds}\n{'='*70}", flush=True)

        # ---- 1. combat agent ----
        cdir = root / f"r{rnd}" / "combat"
        cdir.mkdir(parents=True, exist_ok=True)
        if rnd == 1 and combat_model:
            print(f"[r{rnd}] reusing seed combat model: {combat_model}", flush=True)
        else:
            cmd = [PY, "scripts/train_hierarchical.py", "--phase", "combat",
                   "--total-steps", str(args.combat_steps),
                   "--n-envs", str(args.n_envs),
                   "--eval-freq", str(args.eval_freq),
                   "--eval-episodes", str(args.eval_episodes),
                   "--ascension", str(args.ascension),
                   "--gamma", "0.99", "--device", args.device,
                   "--output-dir", str(cdir)]
            if run_model:
                # Refit on the decks the run agent actually produces.
                decks = root / f"r{rnd}" / "harvested_decks.pkl"
                n = harvest_decks(run_model, combat_model, decks,
                                  args.harvest_episodes, args.ascension,
                                  args.max_act_count)
                print(f"[r{rnd}] harvested {n} real combat-entry decks", flush=True)
                if n:
                    cmd += ["--deck-file", str(decks)]
                else:
                    print(f"[r{rnd}] WARNING harvest empty -- falling back to the "
                          f"synthetic sampler; the combat agent will train on a "
                          f"distribution the run agent may no longer produce.",
                          flush=True)
            rc = launch(cmd, root / f"r{rnd}_combat.log")
            if rc != 0:
                print(f"[r{rnd}] combat training FAILED (rc={rc})", flush=True)
                return rc
            combat_model = str(cdir / "best_model.zip")

        # ---- 2. run agent ----
        rdir = root / f"r{rnd}" / "run"
        rdir.mkdir(parents=True, exist_ok=True)
        cmd = [PY, "scripts/train_hierarchical.py", "--phase", "run",
               "--total-steps", str(args.run_steps),
               "--n-envs", str(args.n_envs),
               "--eval-freq", str(args.eval_freq),
               "--eval-episodes", str(args.eval_episodes),
               "--ascension", str(args.ascension),
               "--max-act-count", str(args.max_act_count),
               "--combat-model", combat_model,
               "--gamma", "0.99", "--device", args.device,
               "--output-dir", str(rdir)]
        rc = launch(cmd, root / f"r{rnd}_run.log")
        if rc != 0:
            print(f"[r{rnd}] run training FAILED (rc={rc})", flush=True)
            return rc
        run_model = str(rdir / "best_model.zip")

        rec = {
            "round": rnd,
            "combat": last_eval(cdir / "eval_history.jsonl"),
            "run": last_eval(rdir / "eval_history.jsonl"),
            "combat_model": combat_model,
            "run_model": run_model,
            "wall_s": round(time.time() - t0, 1),
        }
        with ledger.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(rec) + "\n")
        r_ev = rec["run"]
        print(f"\n[r{rnd}] run agent: floors {r_ev.get('mean_floors', 0):.2f} "
              f"deck {r_ev.get('mean_deck', 0):.2f} "
              f"upgrades {r_ev.get('mean_upgrades', 0):.2f} "
              f"win {r_ev.get('win_rate', 0):.1%}", flush=True)

    print(f"\nledger: {ledger}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
