# Training Redesign — Necrobinder Ascension 10, 95%+ Full-Run Win Rate

Goal: an agent that beats Ascension 10 as Necrobinder on the user's actual game
version (v0.109.0 beta + Acts from the Past + Act 4 Heart mods) in 95%+ of runs,
with fully local inference.

## Why the previous attempt got 0%

1. **Blind observation.** The 131-dim combat vector omits almost everything a
   Necrobinder run depends on: Souls (the character's core resource), Osty's
   HP/state, summons/allies, relics, potions, deck composition, card upgrade
   status, and all but 6 power types. The run-level 20 dims add act/floor/gold
   but no map lookahead, no boss identity, no key-collection state (Act 4 Heart).
   The agent literally cannot see the game it is being asked to master.
2. **Sparse terminal reward over 1000+ step episodes.** +1/-1 at the end of a
   multi-thousand-step episode with compounding deck-building decisions gives
   PPO almost no gradient signal at 1M steps.
3. **Tiny network.** MlpPolicy [256,256] over a lossy vector.
4. **No curriculum.** Training started directly on the hardest configuration.
5. **Training script drift.** `scripts/train_full_run.py` passes `act_count=` /
   `reward_shaping=` kwargs that `STS2RunEnv.__init__` no longer accepts.

## Decision: MaskablePPO with a structured policy net (not an LLM policy)

An LLM-based policy was considered and rejected: RL fine-tuning even a small
(1-4B) open-weight model on the hundreds of millions of env steps this needs is
not feasible on the available hardware (RTX 4060 8GB), and inference at
~thousands of steps/sec during rollout collection is orders of magnitude too
slow. A purpose-built policy network satisfies the same "fully local at
inference time" constraint and is dramatically more compute-efficient for a
fixed, fully-observable-ish, simulator-backed game. If a future phase wants
natural-language priors (e.g. card-text embeddings as auxiliary features), they
can be baked in as frozen embedding features without an LLM in the loop.

## New observation (structured, ~2.5k dims)

- **Cards as entities**: hand (10 slots), draw/discard/exhaust as bag-of-cards
  count vectors over the full CardId space, each card contributing a learned
  embedding + scalar features (cost, upgraded, playable, damage, block,
  ethereal/exhaust flags).
- **Necrobinder state**: Souls, Osty HP/max/alive, ally slots (HP, power
  summary).
- **Full power vectors** for player + each enemy slot (all PowerIds, amount-
  encoded), not a 6-power subset.
- **Relics**: binary vector over RelicId space + counters where stateful.
- **Potions**: slot-wise potion ID + usability.
- **Run level**: act (incl. which legacy-act variant was rolled per slot), floor,
  gold, keys held, boss identity for the current act, map lookahead (room-type
  counts reachable in the next N rows), deck aggregates (size, avg cost,
  upgrade ratio, curse count).
- **Phase one-hot** extended to distinguish reward-sub-screens the old encoding
  merged.

## Policy network

Shared card-embedding encoder (embedding dim ~64) applied to hand slots +
pile bags, mean/attention-pooled, concatenated with the flat features, into a
[1024, 1024, 512] torso with separate policy/value heads. Runs comfortably on
the RTX 4060; rollout collection stays CPU-side in 16-24 SubprocVecEnv workers.

## Reward

Potential-based shaping, annealed toward sparse as win rate rises:

- Terminal: win +1, death -1 (never annealed).
- Act completion: +0.25 per act boss killed (annealed to 0 by stage F).
- Floor progression: +0.004 per floor (annealed).
- Combat efficiency: +0.05 * (hp_end/hp_start) per combat win (annealed).
- No per-kill rewards (avoids stalling exploits).

## Curriculum (stages checkpointed independently)

| Stage | Config | Promotion criterion |
|-------|--------|---------------------|
| A | Combat-only, Act 1 encounters, Necrobinder starter deck, A10 | >85% combat win |
| B | Combat-only, mixed-act encounters incl. legacy-act monsters + Act 4 Heart fights, sampled decks | >75% combat win |
| C | Full run, Act 1 only, A10 | >80% run win |
| D | Full run, Acts 1-2 | >70% run win |
| E | Full run, Acts 1-3 | >60% run win |
| F | Full run, Acts 1-4 (Heart), shaping annealed | target: 95% |

Stages C+ initialize the combat slice of the policy from the stage-B weights
(same action-space prefix by construction).

## Evaluation & checkpointing

- Eval every 100k steps: 200 episodes, fixed held-out seed block, A10
  Necrobinder full config. Track win rate, mean floors, per-act death
  distribution, per-boss win rate.
- Checkpoint every 250k steps + on every eval improvement: model, optimizer
  state, VecNormalize stats (if used), curriculum stage, and the eval history
  JSON. Everything resumable after interruption.
- Final claim of X% requires >=1000 eval episodes.

## Fidelity prerequisites (must be green before stage C)

- Vanilla v0.109.0 drift fixes (done).
- Act 4 Heart content (done).
- Acts from the Past: legacy act monsters (Exordium done; TheCity/TheBeyond in
  progress), legacy act events, act-slot registration, event-pool filtering.
- Full test suite green.
