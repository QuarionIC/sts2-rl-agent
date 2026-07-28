"""RL training for an LLM that makes every out-of-combat run decision.

Scope
-----
The LLM owns the whole run except combat, which the deterministic per-turn
planner plays. That split is deliberate and load-bearing: combat is a solved
optimisation problem given the state (verified live), so training gradient
should not be spent relearning it. What remains -- routing, card picks,
shops, rests, events, boss relics -- is exactly the slice that decided every
plateau this project has hit.

Why this is a policy-gradient problem and not fine-tuning
---------------------------------------------------------
There is no supervised target for "which card should I take". The signal is
how deep the run got, which arrives once per episode after ~20 decisions.
That is textbook sparse-reward RL, so the LLM is treated as a policy over a
discrete action set: each decision is a prompt, and the action is which
numbered option it picks.

Crucially the action space is the simulator's own legal option list, so the
policy cannot emit an illegal move -- only a differently-ranked legal one.
That removes the usual reason LLM-RL setups need heavy output constraints.

Training method
---------------
Sequence-level REINFORCE with a learned baseline, over LoRA adapters:

* **Sequence-level, not token-level.** The decision is the choice, not the
  prose. Credit is assigned to the logprob of the chosen option's token(s),
  which keeps the gradient aimed at the decision rather than at the
  explanation the model writes after it.
* **LoRA.** Full fine-tuning of a 27B model is out of reach on any single
  consumer box, and the base model's game knowledge is the thing worth
  preserving. Adapters keep the trainable parameter count in the tens of
  millions.
* **Learned baseline.** Run depth has high variance (measured ~1.5 floors SE
  at n=8), so raw REINFORCE would be dominated by noise. A value head over
  the prompt encoding gives per-decision advantages.
* **KL anchor to the base policy.** Without it the adapter drifts toward
  degenerate option-0 answers that happen to score once. This is the same
  failure the BC-anchored PPO work in this repo hit earlier.

Reward
------
Terminal reward is run depth, shaped by the same potential-based term the
rest of the project uses (``RewardConfig``), so it stays policy-invariant
and comparable to every prior number. Per-decision credit comes from the
PBRS difference at that decision, not from a hand-authored per-choice score:
the whole point is to learn what a good pick is, not to encode it.

Hardware
--------
Deliberately not runnable on the dev laptop. Qwen3.6-27B Q3_K_M inference
alone needs ~13GB against ~4.4GB free with the game open; training needs
gradients, optimiser state and activations on top. This targets a DGX Spark
(128GB unified) or equivalent. The pipeline is model-agnostic by config so
it can be validated end-to-end with a small model first -- see
``scripts/train_llm_rl.py --smoke``.
"""

from sts2_env.llm_rl.episode import DecisionRecord, EpisodeRollout, collect_episode
from sts2_env.llm_rl.reward import compute_returns, shaped_decision_rewards

__all__ = [
    "DecisionRecord",
    "EpisodeRollout",
    "collect_episode",
    "compute_returns",
    "shaped_decision_rewards",
]
