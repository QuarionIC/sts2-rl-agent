# DGX Spark session, 2026-07-29 — LLM full-decision eval + AlphaZero curriculum

Status doc for the overnight run. Every number here is measured; nothing is
projected. Where a number is preliminary the sample size is stated.

---

## 1. The headline finding: the search was corrupting the live game

`sts2_env/search/combat_mcts.py` `clone_combat` was incomplete, and the
consequence was that **every MCTS and Expert-Iteration result previously
recorded in this project was invalid — including the "search does not help"
NO-GO gate**, which compared a corrupted arm against a clean one.

### How it was found

The recorded NO-GO log contradicted itself: on most seeds MCTS made **zero
action overrides** yet the arms finished 13 vs 2 floors apart. If search never
changes the action, the runs must be identical.

Chain of experiments (each script is kept as the regression test):

| step | result |
|---|---|
| `mcts_purity_check --level combat` | CLEAN, 4/4 identical |
| `mcts_purity_check --level run` | **FAILS 3/4** — seed 10000002 died at 0 HP without search, survived at 34 HP with search-then-discard |
| `mcts_leak_localize` seeds 0,1 | 82 searches, **zero** mutation of HP, block, energy, piles, powers, enemy state, legal mask, every reachable counter-based `Rng`, or numpy's/torch's global generators |
| `mcts_lockstep` seed 10000002 | first divergence at step 57 with the **same action chosen** but obs *and* mask already different, HP and floor still equal → state diverged earlier, outside the first snapshot window |
| `mcts_leak_localize` seed 10000002 | **3 of 60 searches mutate the LIVE HAND** |

The leak, verbatim:

```
step 56: hand 5 -> 8, gained DISCOVERY, VOLLEY, SECRET_TECHNIQUE
step 61: hand 4 -> 7, gained BODYGUARD, DEFEND, DEFEND; draw 9 -> 6
step 63: hand 6 -> 9, gained SCULPTING_STRIKE, CAPTURE_SPIRIT, ...
```

Simulated card **generation** wrote through to the live player: cards appeared
in the live hand and vanished from the live draw pile.

### Root cause

`_fix_leaked_closures` rebuilt reachable closures whose cells referenced
deepcopy'd originals, but three carriers slipped through — all reachable from
card-generation effects:

1. `_rebind_function` mapped cell contents through the deepcopy memo only. A
   cell holding **another function** stayed the original, still closed over the
   live combat. Rebinding is now transitive.
2. **Bound methods**: `types.MethodType` carries its receiver in `__self__`,
   which the attribute walk cannot reach (a method's `__dict__` is its
   function's). Now rebuilt with the remapped receiver.
3. **`functools.partial`**: `func`/`args`/`keywords` are read-only attributes,
   not `__dict__` entries, so the walk could neither see nor replace them. Now
   rebuilt.

Same class of bug as the original "enemy block climbing 0 → 196" that
`clone_combat` was written to fix in the first place.

### Verification

| check | before | after |
|---|---|---|
| leaks, seed 10000002, 60 searches | 3 | **0 of 56** |
| run-level purity, 4 seeds | 3/4 identical | **4/4 identical** |
| test suite | 5461 pass | 5461 pass |

And the invariant that exposed the bug now holds in the live gate:

```
seed 10000006: policy 16 | mcts 16  (0 overrides)   <- identical, as required
```

Before the fix, 0 overrides produced floor 16 vs floor 1.

---

## 2. AlphaZero-style curriculum — feasibility

### What transfers and what does not

AlphaZero proper assumes a deterministic, perfect-information, two-player
zero-sum game. STS2 is single-agent, stochastic (draws, enemy AI rolls) and
imperfect-information (hidden draw order), so **the self-play half has no
analogue**. What transfers is the half that does the work: a search and a
learned network improving each other — Expert Iteration.

Every piece already existed:

* `combat_mcts.py` — determinized PUCT whose evaluator **is** the policy/value
  net (AlphaZero's exact arrangement)
* `distill.py` — masked `CE(policy → visit distribution)` +
  `coef · MSE(value → root value)` (AlphaZero's loss, term for term)
* `exit_distill.py` — collect / distil / eval

`alphazero_curriculum.py` chains them into the iterated loop none of them does
alone, and gates iteration 1 on "does search beat the raw policy".

### Hardware feasibility: confirmed

* torch **2.13.0+cu130**, aarch64, CUDA available, GB10 capability (12, 1),
  **4.3 TFLOP/s fp32** — no source build needed
* sb3 2.9.0 / sb3-contrib 2.9.0 installed
* 20 CPU cores for search (MCTS and the planner are CPU-bound), GPU for
  training — a good fit, better than the laptop

### Early gate results (post-fix, 16 seeds, 48 sims × 8 determinizations)

```
seed 10000006: policy 16 | mcts 16  (0 overrides)
seed 10000001: policy  7 | mcts  7  (1 override)
seed 10000002: policy  7 | mcts 16  won=True  (1 override)   <- loss -> ACT-1 WIN
seed 10000005: policy 13 | mcts 14  (2 overrides)
seed 10000003: policy 12 | mcts 12  (2 overrides)
seed 10000007: policy 13 | mcts 11  (2 overrides)
```

Preliminary and small, but the search now looks like it may genuinely help —
which is the opposite of the invalidated NO-GO. Cost is ~250 s per seed at this
budget.

### Second expert: the beam planner

`planner_distill.py` runs the same loop with the deterministic whole-combat beam
planner as the expert instead of MCTS. Smoke test, 2 episodes / 286 decisions:

| quantity | value |
|---|---|
| planner cost | **2.57 s / decision** |
| policy↔planner agreement | **26.6%** |
| value target range | [−9.47, +0.27] (consistent with win +10 / death −10) |

**The 26.6% agreement is the important number**: the planner picks a different
action 73% of the time, so there is a very large imitation signal available.
This is the cheaper and probably stronger first move — supervised, dense, no
rollout cost — with RL on top afterwards.

---

## 3. LLM playing every decision

### Harness work required first

* No sim-side combat renderer existed. `render_combat_decision` builds the menu
  by **decoding the legal action mask**, so option *i* is env action `legal[i]`
  by construction (the 115-wide combat encoding is not a dense list, so an
  option→index inverse could silently mis-target). Enemy labels carry an index
  (`#0`, `#1`) because duplicate encounters share a `monster_id`.
* A stub that always answers legally then exposed a **pre-existing** bug: 20% of
  *out-of-combat* choices never resolved and were silently replaced by the
  fallback policy. Largest cause: a `choose`/`confirm_choice` prompt raised by a
  non-combat phase is masked into the **combat** slice while the resolver looked
  in that phase's slice, so **every card-selection event decision fell through
  to the knowledge policy** — the model was not making them. Also card-reward
  potion offers (skip at `+3`, not `+1`) and map choices past `map_size`.
  Stub parse rate 96.1% → 100%. **The published 13.13-floor out-of-combat
  baseline is affected and should be re-measured.**
* Combat-outcome accounting scored the **fatal fight as a win** (boundaries were
  only observed on the next `act()`, and `_in_combat` persisted across
  episodes). 16 episodes with 15 deaths reported 97/98 fights won. Fixed;
  4-episode stub check went 18/19 → 15/19, exactly 4 losses for 4 deaths.

### Grammar vs thinking

| config | combat parse | s/decision |
|---|---|---|
| prefill only | **0 / 2** | 20.85 |
| GBNF grammar | **11 / 11** | **1.50** |
| thinking, 1024 tok, free | 2 / 4 | ~100 |
| thinking + budget forcing | **6 / 6** | **43.6** |

The prefill *discourages* reasoning; it cannot prevent it. The model emitted the
closed `<think>` block it was handed and opened a new one, then ran out of
budget mid-thought. A grammar removes the possibility. For the thinking arm,
budget forcing (reason within budget → close the block → force the choice under
grammar in a ~5-token second call) is what makes it measurable at all.

7.9 tok/s is near the memory-bandwidth ceiling for a 28 GB Q8_0 on GB10, so
tokens generated is the only lever that matters.

### Results (grammar-constrained arm, 18 of 30 episodes)

```
mean floors : 8.94 +/- 0.77
median      : 7.5   range 5-16
act-1 wins  : 1/18 = 5.6%   95% CI [1.0%, 25.8%]
```

All arms measured on the Spark, because the simulator is **not** bit-identical
across architectures (§4) — laptop numbers are not a valid reference.

| arm | floors | act-1 wins | 95% CI |
|---|---|---|---|
| random × random | 4.37 ± 0.29 | 0/30 | [0.0%, 11.4%] |
| knowledge × random | 4.00 ± 0.31 | 0/30 | [0.0%, 11.4%] |
| **random × PLANNER** | **11.80 ± 0.67** | **3/30** | [3.5%, 25.6%] |
| LLM × LLM (grammar) | 8.94 ± 0.77 | 1/18 | [1.0%, 25.8%] |

```
LLM-combat    vs random-combat : +4.57 floors  (SE 0.82, z = 5.6)
planner-combat vs LLM-combat   : +2.86 floors  (SE 1.02, z = 2.8)
```

Two conclusions, both clean:

1. **The LLM genuinely plays combat.** +4.57 floors over random combat at
   z = 5.6 is not noise; it is not just following the option list.
2. **The planner is still clearly better.** And note *which* arm beats the LLM:
   `random × planner` has **random** out-of-combat routing and still reaches
   11.80 floors with a 10% win rate. So the planner's combat is worth more than
   the LLM's routing *and* combat combined — combat quality dominates run
   outcome at this stage, and out-of-combat policy barely moves it (4.00 vs 4.37
   for knowledge vs random against the same combat arm).

That is also the strongest argument for planner-distillation (§2): the thing that
matters most is exactly the thing the planner does best.

Caveat to carry with the LLM numbers: the grammar-constrained arm cannot reason
before answering, so this measures "Qwen3.6-27B, forced-format, no visible
deliberation", not the model's ceiling. The thinking arm exists to size that gap.

---

## 4. Cross-architecture nondeterminism (open)

The simulator is **not** bit-identical between x86_64 and aarch64. Fully
scripted walk, no policy, no LLM:

```
x86_64  seed 10000000 -> floor 7, sha256 288422e1...
aarch64 seed 10000000 -> floor 8, sha256 0bc97392...
```

Ruled out: numpy PCG64 streams are identical; the sim is stable across repeat
runs on one machine; independent of `PYTHONHASHSEED`; and aarch64 gives the same
hash under Python 3.12 and 3.13, so it is the architecture. Steps 0–86 are
byte-identical, then **hand order** diverges after a reshuffle with HP, floor
and enemy HP all still matching — pile ordering, not damage math.

Consequence: the laptop-measured baselines (random 10.27, knowledge 10.67,
LLM-ooc 13.13) are **not** a valid reference for anything measured on the Spark.
Hence `--run-policy` × `--combat-policy` on `eval_llm_full.py`, so every arm is
re-measured on the machine in use. Filed as its own task; root cause open.

`output/REWARD_PROVENANCE.json` records the exact reward config and git rev every
in-flight run is measured under, and `sync_spark.sh` now stamps `GIT_REV` so
remote results stop recording "code version : unknown".
