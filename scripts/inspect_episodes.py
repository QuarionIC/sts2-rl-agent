#!/usr/bin/env python3
"""Forensic episode inspector for trained full-run checkpoints.

Replays a checkpoint deterministically in a fresh RichSTS2RunEnv (the live
training run is untouched) and reports what actually happens inside
episodes -- especially the pathological ones.

Examples
--------
Aggregate outcome stats over 100 episodes of the latest G1 checkpoint::

    python scripts/inspect_episodes.py --episodes 100

Trace the tail of every truncated/step-capped episode (what loop is the
policy stuck in?)::

    python scripts/inspect_episodes.py --episodes 100 --trace

Replay one exact episode seen before (same seed => same episode)::

    python scripts/inspect_episodes.py --seeds 20000042 --trace --trace-window 60
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
from collections import Counter
from pathlib import Path

import numpy as np

DEFAULT_CKPT_DIR = Path("output/necrobinder_g1/G1")
DEFAULT_SEED_BASE = 20_000_000


def latest_checkpoint(ckpt_dir: Path) -> Path:
    """Newest model zip in a stage dir (best_model.zip or ckpt_*.zip)."""
    candidates = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return candidates[-1]


def describe_env_action(env, action: int) -> str:
    """Human-readable description of a unified-action-space index, resolved
    against the CURRENT state (mirrors run_env's _step_* dispatch)."""
    from sts2_env.cli.play_run import describe_action
    from sts2_env.gym_env.run_env import _LAYOUT
    from sts2_env.gym_env.action_space import (
        action_to_card_and_target,
        action_to_potion_and_target,
        is_potion_action,
    )
    from sts2_env.run.run_manager import RunManager

    mgr = env._mgr
    if mgr is None:
        return f"action {action}"
    phase = mgr.phase
    actions = mgr.get_available_actions()

    def nth(filtered, local):
        if not filtered:
            return None
        return filtered[max(0, min(local, len(filtered) - 1))]

    try:
        if phase != RunManager.PHASE_COMBAT and any(
            a.get("action") in {"choose", "confirm_choice"} for a in actions
        ):
            local = max(0, min(action - _LAYOUT.combat_start, _LAYOUT.combat_size - 1))
            if local == 0:
                return "[choice] confirm/skip"
            chosen = nth([a for a in actions if a.get("action") == "choose"], local - 1)
            return f"[choice] {describe_action(chosen)}" if chosen else f"[choice] toggle #{local - 1}"
        if phase == RunManager.PHASE_COMBAT:
            if _LAYOUT.player_select_start <= action < _LAYOUT.player_select_start + _LAYOUT.player_select_size:
                return "[combat] select acting player"
            local = max(0, min(action - _LAYOUT.combat_start, _LAYOUT.combat_size - 1))
            combat = mgr.get_combat_state()
            if combat is not None and combat.pending_choice is not None:
                return "[combat-choice] confirm" if local == 0 else f"[combat-choice] toggle option {local - 1}"
            if local == 0:
                return "[combat] END TURN"
            if is_potion_action(local):
                slot, tgt = action_to_potion_and_target(local)
                return f"[combat] potion slot {slot}" + (f" -> enemy {tgt}" if tgt is not None else "")
            hand_idx, tgt = action_to_card_and_target(local)
            name = ""
            if combat is not None and hand_idx is not None and hand_idx < len(combat.hand):
                name = f" ({combat.hand[hand_idx]!r})"
            return f"[combat] play hand[{hand_idx}]{name}" + (f" -> enemy {tgt}" if tgt is not None else "")
        if phase == RunManager.PHASE_MAP_CHOICE:
            chosen = nth(actions, action - _LAYOUT.map_start)
            return f"[map] {describe_action(chosen)}" if chosen else "[map] move"
        # Remaining phases: report the phase plus the resolved concrete action
        # where the slice arithmetic is simple.
        slice_names = [
            (_LAYOUT.card_reward_start, _LAYOUT.card_reward_size, "card-reward"),
            (_LAYOUT.card_reward_extra_start, _LAYOUT.card_reward_extra_size, "card-reward-extra"),
            (_LAYOUT.boss_relic_start, _LAYOUT.boss_relic_size, "boss-relic"),
            (_LAYOUT.shop_start, _LAYOUT.shop_size, "shop"),
            (_LAYOUT.rest_start, _LAYOUT.rest_size, "rest"),
            (_LAYOUT.event_start, _LAYOUT.event_size, "event"),
            (_LAYOUT.treasure_start, _LAYOUT.treasure_size, "treasure/reroll"),
        ]
        for start, size, name in slice_names:
            if start <= action < start + size:
                return f"[{name}] local index {action - start} (phase={phase})"
        return f"action {action} (phase={phase})"
    except Exception as exc:  # forensics must never crash the replay
        return f"action {action} (describe failed: {exc})"


def find_cycle(tail: list[str]) -> tuple[int, list[str]] | None:
    """Detect the shortest action-pattern cycle at the end of a trace."""
    for period in range(1, min(12, len(tail) // 3) + 1):
        pattern = tail[-period:]
        repeats = 0
        idx = len(tail) - period
        while idx - period >= 0 and tail[idx - period: idx] == pattern:
            repeats += 1
            idx -= period
        if repeats >= 2:
            return period, pattern
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--checkpoint", default=None, help="Model zip (default: newest in --ckpt-dir)")
    parser.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    parser.add_argument("--episodes", type=int, default=50)
    parser.add_argument("--seeds", type=int, nargs="*", default=None,
                        help="Explicit episode seeds (overrides --episodes/--seed-base)")
    parser.add_argument("--seed-base", type=int, default=DEFAULT_SEED_BASE)
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--acts", type=int, default=2, help="max_act_count (win condition)")
    parser.add_argument("--max-steps", type=int, default=3000)
    parser.add_argument("--trace", action="store_true",
                        help="Print the trailing actions of every truncated/step-capped episode")
    parser.add_argument("--trace-window", type=int, default=40)
    args = parser.parse_args()

    import sts2_env.events  # noqa: F401  (registry side effects)
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint: {ckpt}")
    model = MaskablePPO.load(str(ckpt), device="cpu")

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        max_act_count=args.acts,
    )
    env.set_shaping_scale(0.0)

    seeds = args.seeds if args.seeds else [args.seed_base + i for i in range(args.episodes)]

    outcomes = Counter()
    dead_floors: list[int] = []
    trunc_floors: list[int] = []
    stuck_phase = Counter()

    for seed in seeds:
        obs, info = env.reset(seed=seed)
        trace: list[tuple[str, str]] = []  # (phase, described action)
        done = trunc = False
        steps = 0
        while not (done or trunc) and steps < args.max_steps:
            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            desc = describe_env_action(env, int(action)) if args.trace else ""
            phase = info.get("phase", "?")
            obs, reward, done, trunc, info = env.step(int(action))
            if args.trace:
                trace.append((str(phase), desc))
            steps += 1

        floor = int(info.get("floor", 0))
        if done and info.get("won", False):
            outcomes["win"] += 1
        elif done:
            outcomes["death"] += 1
            dead_floors.append(floor)
        else:
            outcomes["truncated"] += 1
            trunc_floors.append(floor)
            stuck_phase[str(info.get("phase", "?"))] += 1
            if args.trace:
                window = trace[-args.trace_window:]
                print(f"\n=== TRUNCATED episode seed={seed} floor={floor} "
                      f"stuck_phase={info.get('phase')} steps={steps} ===")
                cycle = find_cycle([d for _, d in window])
                if cycle:
                    period, pattern = cycle
                    print(f"  CYCLE DETECTED (period {period}):")
                    for entry in pattern:
                        print(f"    {entry}")
                    print(f"  ... repeating through the final {args.trace_window} steps. Full tail:")
                for phase, desc in window[-12:]:
                    print(f"    [{phase}] {desc}")

    total = sum(outcomes.values())
    print(f"\n=== SUMMARY ({total} episodes, asc {args.ascension}, acts {args.acts}) ===")
    print(f"wins:       {outcomes['win']}")
    print(f"deaths:     {outcomes['death']}"
          + (f"  (mean floor {np.mean(dead_floors):.2f})" if dead_floors else ""))
    print(f"truncated:  {outcomes['truncated']}"
          + (f"  (mean floor {np.mean(trunc_floors):.2f})" if trunc_floors else ""))
    if stuck_phase:
        print(f"stuck phases: {dict(stuck_phase)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
