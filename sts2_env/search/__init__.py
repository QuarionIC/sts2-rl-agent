"""Simulator search (TRAINING_REVAMP_SPEC Phase 8).

:mod:`sts2_env.search.combat_mcts` -- determinized PUCT MCTS over
deepcopy'd :class:`~sts2_env.core.combat.CombatState` for a single combat
decision (offline ExIt data generation + inference-time search).

:mod:`sts2_env.search.distill` -- Expert Iteration distillation losses
(masked CE to MCTS visit distributions + value MSE to root values).
"""
