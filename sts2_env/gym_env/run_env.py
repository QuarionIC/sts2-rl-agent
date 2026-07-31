"""STS2 Full-Run Gymnasium Environment.

Wraps :class:`RunManager` to expose a complete STS2 run (Acts 0-2) as a
single Gymnasium episode.  Each ``step()`` call corresponds to one player
decision; non-interactive transitions (treasure collection, post-combat
bookkeeping, act transitions) are handled internally by RunManager.

Action space
------------
``Discrete(157)`` with **action masking** so that only the actions valid
for the current game phase are unmasked:

===========  ========  ====================================================
 Slice        Phase     Meaning
===========  ========  ====================================================
 0-114        COMBAT    Card/end-turn/potion actions from combat env
 115-119      MAP       Map choice 0-4
 120-123      CARD_RWD  Card reward pick 0-2, or skip
 124-126      CARD_RWD  Extra card reward picks 3-5 when present
 127-129      BOSS_REL  Boss relic choice 0-2
 130-139      SHOP      Shop action 0-9
 140-144      REST      Rest-site option 0-4
 145-148      EVENT     Event option 0-3
 149          TREASURE  Take and continue
 149          CARD_RWD  Reroll current card reward when available
 150-156      COMBAT    Select acting player 0-6
===========  ========  ====================================================

Observation space
-----------------
Flat ``float32`` vector of size ``RUN_OBS_SIZE`` (151).

* Combat observation (131) -- reuses :func:`encode_observation`.
* Run-level state (20):
  - current_act, total_floor, and act_floor normalized by run scales (3)
  - player HP ratio and normalized gold                             (2)
  - normalized deck size and relic count                            (2)
  - num_potions / max_potion_slots (or 0)                        (2)
  - phase one-hot (8 phases)                                     (8)
  - ascension_level / 20                                         (1)
  - is_elite flag, is_boss flag                                  (2)

Reward
------
Sparse: **+1** for winning the run, **-1** for death or timeout.

Compatibility
-------------
Implements ``action_masks()`` for *sb3-contrib* ``MaskablePPO``.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass
from typing import Any

import gymnasium
import numpy as np
from gymnasium import spaces

from sts2_env.core.constants import (
    ACTION_SPACE_SIZE as COMBAT_ACTION_SPACE_SIZE,
    MAX_ENEMIES,
    MAX_HAND_SIZE,
)
from sts2_env.core.enums import RoomType
from sts2_env.gym_env.action_space import (
    action_to_card_and_target,
    action_to_potion_and_target,
    get_action_mask,
    is_potion_action,
)
from sts2_env.gym_env.observation import OBS_SIZE as COMBAT_OBS_SIZE, encode_observation
from sts2_env.core.rng import INT_MAX_EXCLUSIVE
from sts2_env.run.run_manager import RunManager

@dataclass(frozen=True)
class _ActionLayout:
    combat_start: int
    combat_size: int
    map_start: int
    map_size: int
    card_reward_start: int
    card_reward_size: int
    card_reward_extra_start: int
    card_reward_extra_size: int
    boss_relic_start: int
    boss_relic_size: int
    shop_start: int
    shop_size: int
    rest_start: int
    rest_size: int
    event_start: int
    event_size: int
    treasure_start: int
    treasure_size: int
    player_select_start: int
    player_select_size: int

    @property
    def card_reward_reroll(self) -> int:
        return self.treasure_start

    @property
    def total_actions(self) -> int:
        return self.player_select_start + self.player_select_size


def _build_action_layout() -> _ActionLayout:
    combat_start = 0
    combat_size = COMBAT_ACTION_SPACE_SIZE
    map_start = combat_start + combat_size
    map_size = 5
    card_reward_start = map_start + map_size
    card_reward_size = 4
    card_reward_extra_start = card_reward_start + card_reward_size
    card_reward_extra_size = 3
    boss_relic_start = card_reward_extra_start + card_reward_extra_size
    boss_relic_size = 3
    shop_start = boss_relic_start + boss_relic_size
    shop_size = 10
    rest_start = shop_start + shop_size
    rest_size = 5
    event_start = rest_start + rest_size
    event_size = 4
    treasure_start = event_start + event_size
    # 0 = collect, 1 = SKIP. Skipping a treasure chest is not a null move:
    # in Act 3 it is how the Sapphire (blue) key is obtained, and all three
    # keys are required to reach Act 4. RunManager already offered a "skip"
    # action whenever the reward is skippable; the action space simply never
    # exposed it, so the agent could not decline a chest and could never
    # unlock the Act 4 path.
    treasure_size = 2
    player_select_start = treasure_start + treasure_size
    player_select_size = 7
    return _ActionLayout(
        combat_start=combat_start,
        combat_size=combat_size,
        map_start=map_start,
        map_size=map_size,
        card_reward_start=card_reward_start,
        card_reward_size=card_reward_size,
        card_reward_extra_start=card_reward_extra_start,
        card_reward_extra_size=card_reward_extra_size,
        boss_relic_start=boss_relic_start,
        boss_relic_size=boss_relic_size,
        shop_start=shop_start,
        shop_size=shop_size,
        rest_start=rest_start,
        rest_size=rest_size,
        event_start=event_start,
        event_size=event_size,
        treasure_start=treasure_start,
        treasure_size=treasure_size,
        player_select_start=player_select_start,
        player_select_size=player_select_size,
    )


# ---------------------------------------------------------------------------
# Unified action-space layout
# ---------------------------------------------------------------------------

_LAYOUT = _build_action_layout()
TOTAL_ACTIONS = _LAYOUT.total_actions

# Export the legacy names that tests and callers already import.
_COMBAT_START = _LAYOUT.combat_start
_COMBAT_SIZE = _LAYOUT.combat_size
_MAP_START = _LAYOUT.map_start
_MAP_SIZE = _LAYOUT.map_size
_CARD_RWD_START = _LAYOUT.card_reward_start
_CARD_RWD_SIZE = _LAYOUT.card_reward_size
_CARD_RWD_EXTRA_START = _LAYOUT.card_reward_extra_start
_CARD_RWD_EXTRA_SIZE = _LAYOUT.card_reward_extra_size
_BOSS_RELIC_START = _LAYOUT.boss_relic_start
_BOSS_RELIC_SIZE = _LAYOUT.boss_relic_size
_SHOP_START = _LAYOUT.shop_start
_SHOP_SIZE = _LAYOUT.shop_size
_REST_START = _LAYOUT.rest_start
_REST_SIZE = _LAYOUT.rest_size
_EVENT_START = _LAYOUT.event_start
_EVENT_SIZE = _LAYOUT.event_size
_TREASURE_START = _LAYOUT.treasure_start
_TREASURE_SIZE = _LAYOUT.treasure_size
_CARD_RWD_REROLL = _LAYOUT.card_reward_reroll
_PLAYER_SELECT_START = _LAYOUT.player_select_start
_PLAYER_SELECT_SIZE = _LAYOUT.player_select_size

# ---------------------------------------------------------------------------
# Phase index for one-hot encoding (8 phases)
# ---------------------------------------------------------------------------

_PHASE_INDEX: dict[str, int] = {
    RunManager.PHASE_MAP_CHOICE: 0,
    RunManager.PHASE_COMBAT:     1,
    RunManager.PHASE_CARD_REWARD: 2,
    RunManager.PHASE_BOSS_RELIC:  3,
    RunManager.PHASE_SHOP:        4,
    RunManager.PHASE_REST_SITE:   5,
    RunManager.PHASE_EVENT:       6,
    RunManager.PHASE_TREASURE:    7,
}
NUM_PHASES = len(_PHASE_INDEX)

# ---------------------------------------------------------------------------
# Observation size
# ---------------------------------------------------------------------------

_RUN_STATE_SIZE = 20   # see module docstring
RUN_OBS_SIZE = COMBAT_OBS_SIZE + _RUN_STATE_SIZE  # 131 + 20 = 151

DEFAULT_MAX_STEPS = 10_000
DEFAULT_MAX_COMBAT_TURNS = 200

#: Per-combat env-step cap. 30 turns x ~10 decisions/turn is generous for any
#: legitimate fight; only within-turn pending-choice loops exceed this.
MAX_STEPS_PER_COMBAT = 400

OBS_VALUE_LOW = -1.0
OBS_VALUE_HIGH = 10.0
OBS_CURRENT_ACT_SCALE = 3.0
OBS_TOTAL_FLOOR_SCALE = 50.0
OBS_ACT_FLOOR_SCALE = 20.0
OBS_GOLD_SCALE = 1000.0
OBS_DECK_SIZE_SCALE = 40.0
OBS_RELIC_COUNT_SCALE = 30.0
OBS_MAX_POTION_SLOTS_SCALE = 5.0
OBS_ASCENSION_SCALE = 20.0

# ---------------------------------------------------------------------------
# Reward constants
# ---------------------------------------------------------------------------

REWARD_WIN = 1.0
REWARD_DEATH = -1.0

logger = logging.getLogger(__name__)


class STS2RunEnv(gymnasium.Env):
    """Gymnasium environment for a complete STS2 run.

    Wraps :class:`RunManager` with a ``Discrete(157)`` action space and
    action masking suitable for ``MaskablePPO``.

    Parameters
    ----------
    character_id : str
        Character to play (``"Ironclad"``, ``"Silent"``, etc.).
    ascension_level : int
        Ascension level (0-20).
    max_steps : int
        Maximum environment steps before the episode is truncated.
    max_combat_turns : int
        Maximum turns within a single combat before it is force-ended
        as a loss.
    render_mode : str or None
        ``"ansi"`` for text rendering.
    """

    metadata = {"render_modes": ["ansi"]}

    def __init__(
        self,
        character_id: str = "Ironclad",
        ascension_level: int = 0,
        max_steps: int = DEFAULT_MAX_STEPS,
        max_combat_turns: int = DEFAULT_MAX_COMBAT_TURNS,
        render_mode: str | None = None,
    ):
        super().__init__()

        self.observation_space = spaces.Box(
            low=OBS_VALUE_LOW,
            high=OBS_VALUE_HIGH,
            shape=(RUN_OBS_SIZE,),
            dtype=np.float32,
        )
        self.action_space = spaces.Discrete(_LAYOUT.total_actions)

        self._character_id = character_id
        self._ascension_level = ascension_level
        self.max_steps = max_steps
        self.max_combat_turns = max_combat_turns
        # Per-combat step counting for MAX_STEPS_PER_COMBAT (see _step_combat).
        self._steps_combat_ref: object | None = None
        self._steps_in_combat: int = 0
        # Anti-dither bookkeeping for pending-choice screens (see
        # _limit_choice_mask): per-option toggle counts + total steps spent on
        # the CURRENT choice screen (identity-tracked; reset on screen change).
        self._choice_ref: object | None = None
        self._choice_toggles: dict[int, int] = {}
        self._choice_steps: int = 0
        self.render_mode = render_mode

        # Mutable state -- set during reset()
        self._mgr: RunManager | None = None
        self._step_count: int = 0

    # ------------------------------------------------------------------
    # Gymnasium API
    # ------------------------------------------------------------------

    def reset(
        self,
        seed: int | None = None,
        options: dict[str, Any] | None = None,
    ) -> tuple[np.ndarray, dict[str, Any]]:
        super().reset(seed=seed)

        run_seed = int(self.np_random.integers(0, INT_MAX_EXCLUSIVE))
        self._mgr = RunManager(
            seed=run_seed,
            character_id=self._character_id,
            ascension_level=self._ascension_level,
        )
        self._step_count = 0

        obs = self._encode_obs()
        info = self._build_info()
        return obs, info

    def step(
        self, action: int,
    ) -> tuple[np.ndarray, float, bool, bool, dict[str, Any]]:
        assert self._mgr is not None, "Must call reset() before step()"
        self._step_count += 1

        reward = 0.0
        phase = self._mgr.phase
        actions = self._mgr.get_available_actions()
        sim_error = False

        # ---- dispatch action to RunManager ----
        try:
            if phase != RunManager.PHASE_COMBAT and any(a.get("action") in {"choose", "confirm_choice"} for a in actions):
                self._step_noncombat_choice(action)
            elif phase == RunManager.PHASE_COMBAT:
                self._step_combat(action)
            elif phase == RunManager.PHASE_MAP_CHOICE:
                self._step_map_choice(action)
            elif phase == RunManager.PHASE_CARD_REWARD:
                self._step_card_reward(action)
            elif phase == RunManager.PHASE_BOSS_RELIC:
                self._step_boss_relic(action)
            elif phase == RunManager.PHASE_SHOP:
                self._step_shop(action)
            elif phase == RunManager.PHASE_REST_SITE:
                self._step_rest_site(action)
            elif phase == RunManager.PHASE_EVENT:
                self._step_event(action)
            elif phase == RunManager.PHASE_TREASURE:
                self._step_treasure(action - _TREASURE_START)
        except Exception:
            # Guard against simulation bugs so the episode can finish.
            # Force-end as a loss if the run is not already over, but tag
            # it so callers can avoid scoring a simulator bug as a death.
            logger.exception("STS2RunEnv.step failed during phase %s with action %s", phase, action)
            sim_error = True
            if not self._mgr.is_over:
                self._mgr.run_state.lose_run()

        # ---- terminal conditions ----
        terminated = self._mgr.is_over
        truncated = False

        if not terminated and self._step_count >= self.max_steps:
            truncated = True

        if terminated:
            if sim_error:
                reward = 0.0  # simulator bug, not a real death
            else:
                reward = REWARD_WIN if self._mgr.player_won else REWARD_DEATH
        elif truncated:
            reward = REWARD_DEATH

        obs = self._encode_obs()
        info = self._build_info()
        if sim_error:
            info["sim_error"] = True
        return obs, float(reward), terminated, truncated, info

    # ------------------------------------------------------------------
    # Anti-dither guard for pending-choice screens
    # ------------------------------------------------------------------
    #
    # Observed pathology (forensics via scripts/inspect_episodes.py): the
    # deterministic policy toggles the same option selected/deselected
    # forever on "choose a card" screens -- an absorbing argmax 2-cycle that
    # burns the episode step cap, hangs evals, and wastes training steps.
    # The guard makes every choice screen terminate in bounded decisions
    # while keeping every OUTCOME reachable:
    #   - an option toggled >= 2 times on the same screen is masked out
    #     (re-toggling it cannot produce a new state);
    #   - after a hard step budget on one screen, only confirm remains
    #     legal (or the least-toggled option when confirm isn't yet legal,
    #     e.g. min-select unmet -- confirm becomes forced right after);
    #   - if the rules would leave no legal action, counts reset (never
    #     deadlocks).

    #: Per-option toggle limit on one choice screen.
    CHOICE_TOGGLE_LIMIT = 2
    #: Hard step budget per choice screen before confirm is forced.
    CHOICE_STEP_BUDGET_PER_OPTION = 4
    CHOICE_STEP_BUDGET_BASE = 8

    def _active_choice_object(self) -> object | None:
        """The live pending-choice object (run-level or combat-level)."""
        mgr = self._mgr
        if mgr is None:
            return None
        pending = getattr(mgr.run_state, "pending_choice", None)
        if pending is not None:
            return pending
        combat = mgr.get_combat_state()
        if combat is not None and combat.pending_choice is not None:
            return combat.pending_choice
        event = getattr(mgr, "_event_model", None)
        if event is not None and getattr(event, "pending_choice", None) is not None:
            return event.pending_choice
        return None

    def _sync_choice_tracking(self, choice_obj: object | None) -> None:
        if choice_obj is not self._choice_ref:
            self._choice_ref = choice_obj
            self._choice_toggles = {}
            self._choice_steps = 0

    def _note_choice_action(self, local: int) -> None:
        """Record one decision on the current choice screen (local 0 =
        confirm, local >= 1 = toggle option local-1)."""
        self._sync_choice_tracking(self._active_choice_object())
        if self._choice_ref is None:
            return
        self._choice_steps += 1
        if local >= 1:
            self._choice_toggles[local - 1] = self._choice_toggles.get(local - 1, 0) + 1

    def _limit_choice_mask(
        self,
        mask: np.ndarray,
        base: int,
        n_options: int,
        can_confirm: bool,
        choice_obj: object | None,
        selected_flags: list[bool] | None = None,
    ) -> None:
        self._sync_choice_tracking(choice_obj)
        if choice_obj is None or n_options <= 0:
            return
        budget = self.CHOICE_STEP_BUDGET_BASE + self.CHOICE_STEP_BUDGET_PER_OPTION * n_options
        original = mask[base: base + 1 + n_options].copy()

        if self._choice_steps >= budget:
            # Hard cap: force resolution. Confirm-only when legal; else keep
            # ONE option -- preferring an UNSELECTED one so the forced toggle
            # creates a selection and makes confirm legal on the next step
            # (min-select screens have confirm illegal at zero selections).
            if can_confirm:
                mask[base + 1: base + 1 + n_options] = 0
            else:
                legal = [i for i in range(n_options) if mask[base + 1 + i]]
                if legal:
                    unselected = [
                        i for i in legal
                        if selected_flags is None or not (i < len(selected_flags) and selected_flags[i])
                    ]
                    pool = unselected or legal
                    keep = min(pool, key=lambda i: self._choice_toggles.get(i, 0))
                    mask[base + 1: base + 1 + n_options] = 0
                    mask[base + 1 + keep] = 1
        else:
            for i in range(n_options):
                if self._choice_toggles.get(i, 0) >= self.CHOICE_TOGGLE_LIMIT:
                    mask[base + 1 + i] = 0

        if mask[base: base + 1 + n_options].sum() == 0:
            # Never deadlock: restore the mask and allow re-toggling, but KEEP
            # the step budget accruing -- resetting it here previously created
            # a period-2N meta-cycle (exhaust toggles -> reset -> repeat) in
            # which the hard cap could never fire.
            mask[base: base + 1 + n_options] = original
            self._choice_toggles = {}

    def action_masks(self) -> np.ndarray:
        """Return a boolean mask over the unified discrete action space.

        Required by *sb3-contrib* ``MaskablePPO``.
        """
        layout = _LAYOUT
        mask = np.zeros(layout.total_actions, dtype=np.int8)

        if self._mgr is None or self._mgr.is_over:
            # Fallback: unmask a single action so sampling never fails.
            mask[0] = 1
            return mask

        phase = self._mgr.phase
        actions = self._mgr.get_available_actions()

        if phase != RunManager.PHASE_COMBAT and any(a.get("action") in {"choose", "confirm_choice"} for a in actions):
            can_confirm = any(a.get("action") == "confirm_choice" for a in actions)
            if can_confirm:
                mask[layout.combat_start] = 1
            choose_actions = [a for a in actions if a.get("action") == "choose"]
            n_options = min(len(choose_actions), layout.combat_size - 1)
            for i in range(n_options):
                mask[layout.combat_start + 1 + i] = 1
            self._limit_choice_mask(
                mask, layout.combat_start, n_options, can_confirm,
                self._active_choice_object(),
                selected_flags=[bool(a.get("selected")) for a in choose_actions[:n_options]],
            )
        elif phase == RunManager.PHASE_COMBAT:
            combat = self._mgr.get_combat_state()
            if combat is not None:
                selected_action = next(
                    (a for a in actions if a.get("action") == "select_player" and a.get("selected")),
                    None,
                )
                selected_owner = combat.primary_player
                if selected_action is not None:
                    for state in combat.combat_player_states:
                        if state.player_state.player_id == selected_action.get("player_id"):
                            selected_owner = state.creature
                            break
                combat_mask = get_action_mask(combat, owner=selected_owner)
                available_combat_slots = max(0, len(mask) - layout.combat_start)
                n = min(len(combat_mask), layout.combat_size, available_combat_slots)
                mask[layout.combat_start: layout.combat_start + n] = combat_mask[:n]
                if combat.pending_choice is not None:
                    pc = combat.pending_choice
                    n_pc = min(pc.num_options, layout.combat_size - 1)
                    sel = getattr(pc, "selected_indices", None)
                    self._limit_choice_mask(
                        mask, layout.combat_start, n_pc,
                        bool(mask[layout.combat_start]),
                        pc,
                        selected_flags=[i in sel for i in range(n_pc)] if sel is not None else None,
                    )
                select_actions = [
                    a for a in actions if a.get("action") == "select_player"
                ]
                for i in range(min(len(select_actions), layout.player_select_size)):
                    mask[layout.player_select_start + i] = 1
            else:
                mask[layout.combat_start] = 1  # end turn fallback

        elif phase == RunManager.PHASE_MAP_CHOICE:
            n = min(len(actions), layout.map_size)
            for i in range(n):
                mask[layout.map_start + i] = 1

        elif phase == RunManager.PHASE_CARD_REWARD:
            if any(a.get("action") == "pick_potion" for a in actions):
                mask[layout.card_reward_start] = 1
                mask[layout.card_reward_start + 3] = 1
            elif any(a.get("action") == "pick_relic_reward" for a in actions):
                mask[layout.card_reward_start] = 1
                mask[layout.card_reward_start + 3] = 1
            else:
                mask[layout.card_reward_start + 3] = 1
                pick_actions = [a for a in actions if a.get("action") == "pick_card"]
                for i in range(min(len(pick_actions), 3)):
                    mask[layout.card_reward_start + i] = 1
                extra_cards = max(0, len(pick_actions) - 3)
                for i in range(min(extra_cards, layout.card_reward_extra_size)):
                    mask[layout.card_reward_extra_start + i] = 1
                if any(a.get("action") == "reroll_card_reward" for a in actions):
                    mask[layout.card_reward_reroll] = 1

        elif phase == RunManager.PHASE_BOSS_RELIC:
            n = min(
                sum(1 for a in actions if a.get("action") == "pick_relic"),
                layout.boss_relic_size,
            )
            for i in range(n):
                mask[layout.boss_relic_start + i] = 1

        elif phase == RunManager.PHASE_SHOP:
            # Action 0 = leave (always valid when in shop)
            mask[layout.shop_start] = 1
            self._mask_shop(actions, mask)

        elif phase == RunManager.PHASE_REST_SITE:
            rest_actions = [
                a for a in actions if a.get("action") == "rest_option"
            ]
            for i in range(min(len(rest_actions), layout.rest_size)):
                mask[layout.rest_start + i] = 1

        elif phase == RunManager.PHASE_EVENT:
            event_actions = [
                a for a in actions if a.get("action") == "event_choice"
            ]
            for i in range(min(len(event_actions), layout.event_size)):
                mask[layout.event_start + i] = 1

        elif phase == RunManager.PHASE_TREASURE:
            mask[layout.treasure_start] = 1
            # Only unmask SKIP when the manager actually offers it. Chests
            # differ: a skippable one is the Sapphire-key opportunity, an
            # unskippable one must be taken, and masking them alike would
            # teach the agent that skipping is sometimes a no-op.
            if any(a.get("action") == "skip"
                   for a in self._mgr.get_available_actions()):
                mask[layout.treasure_start + 1] = 1

        # Safety: guarantee at least one action is unmasked.
        if mask.sum() == 0:
            mask[0] = 1

        return mask

    # ------------------------------------------------------------------
    # Action dispatch helpers
    # ------------------------------------------------------------------

    def _step_combat(self, action: int) -> None:
        """Translate a unified action index into a RunManager combat action."""
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        combat = mgr.get_combat_state()
        if combat is None or combat.is_over:
            return

        if layout.player_select_start <= action < layout.player_select_start + layout.player_select_size:
            select_actions = [
                a for a in mgr.get_available_actions()
                if a.get("action") == "select_player"
            ]
            idx = action - layout.player_select_start
            if 0 <= idx < len(select_actions):
                mgr.take_action(select_actions[idx])
            return

        local = max(0, min(action - layout.combat_start, layout.combat_size - 1))
        if combat.pending_choice is not None:
            self._note_choice_action(local)
            if local == 0:
                mgr.take_action({"action": "confirm_choice"})
            else:
                mgr.take_action({"action": "choose", "index": local - 1})
        else:
            if is_potion_action(local):
                slot_idx, target_idx = action_to_potion_and_target(local)
                if slot_idx is not None:
                    combat_now = mgr.get_combat_state()
                    if combat_now is not None and not combat_now.is_over:
                        combat_now.use_potion(slot_idx, target_index=target_idx)
            else:
                hand_idx, target_idx = action_to_card_and_target(local)

                if hand_idx is None:
                    mgr.take_action({"action": "end_turn"})
                else:
                    act: dict[str, Any] = {
                        "action": "play_card",
                        "hand_index": hand_idx,
                    }
                    if target_idx is not None:
                        act["target_index"] = target_idx
                    result = mgr.take_action(act)
                    if not result.get("success", True) and not mgr.is_over:
                        if mgr.phase == RunManager.PHASE_COMBAT:
                            combat2 = mgr.get_combat_state()
                            if combat2 is not None and not combat2.is_over:
                                mgr.take_action({"action": "end_turn"})

        # Force-end combat if it exceeds the turn limit, OR the per-combat
        # STEP limit. The turn cap alone has a hole: an agent looping a
        # pending-choice toggle WITHIN one turn never ends the turn, so
        # turn_count freezes while env steps burn to the episode cap
        # (observed: truncated eval episodes stuck in COMBAT phase). Both
        # caps score as a death: taking absurdly long = losing.
        if mgr.phase == RunManager.PHASE_COMBAT:
            combat_now = mgr.get_combat_state()
            if combat_now is not None and not combat_now.is_over:
                if combat_now is not self._steps_combat_ref:
                    self._steps_combat_ref = combat_now
                    self._steps_in_combat = 0
                self._steps_in_combat += 1
                if (
                    combat_now.turn_count > self.max_combat_turns
                    or self._steps_in_combat > MAX_STEPS_PER_COMBAT
                ):
                    mgr.run_state.player.current_hp = 0
                    mgr.run_state.lose_run()

    def _step_map_choice(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        actions = mgr.get_available_actions()
        if not actions:
            return
        local = max(0, min(action - layout.map_start, len(actions) - 1))
        mgr.take_action(actions[local])

    def _step_card_reward(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        actions = mgr.get_available_actions()
        if any(a.get("action") == "pick_potion" for a in actions):
            local = action - layout.card_reward_start
            if local == 0:
                mgr.take_action({"action": "pick_potion"})
            else:
                mgr.take_action({"action": "skip_potion"})
            return
        if any(a.get("action") == "pick_relic_reward" for a in actions):
            local = action - layout.card_reward_start
            if local == 0:
                mgr.take_action({"action": "pick_relic_reward"})
            else:
                mgr.take_action({"action": "skip_relic"})
            return

        if action == layout.card_reward_reroll and any(a.get("action") == "reroll_card_reward" for a in actions):
            mgr.take_action({"action": "reroll_card_reward"})
            return

        if layout.card_reward_extra_start <= action < layout.card_reward_extra_start + layout.card_reward_extra_size:
            mgr.take_action({"action": "pick_card", "index": 3 + (action - layout.card_reward_extra_start)})
            return

        local = action - layout.card_reward_start

        if local == 3 or local < 0 or local > 3:
            # Skip
            mgr.take_action({"action": "skip"})
        else:
            mgr.take_action({"action": "pick_card", "index": local})

    def _step_boss_relic(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        local = max(0, min(action - layout.boss_relic_start, layout.boss_relic_size - 1))
        mgr.take_action({"action": "pick_relic", "index": local})

    def _step_shop(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        local = action - layout.shop_start

        if local <= 0 or local >= layout.shop_size:
            mgr.take_action({"action": "leave_shop"})
            return

        # Map local index to a concrete shop action from RunManager.
        # We build a canonical ordered list of buyable actions and pick by
        # index.  Action 0 = leave (handled above), 1-N = buy items.
        buyable = self._get_shop_buy_actions()
        idx = local - 1
        if 0 <= idx < len(buyable):
            mgr.take_action(buyable[idx])
        else:
            # Index out of range -- just leave the shop.
            mgr.take_action({"action": "leave_shop"})

    def _step_rest_site(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        actions = [
            a for a in mgr.get_available_actions()
            if a.get("action") == "rest_option"
        ]
        if not actions:
            return
        local = max(0, min(action - layout.rest_start, len(actions) - 1))
        mgr.take_action(actions[local])

    def _step_event(self, action: int) -> None:
        layout = _LAYOUT
        mgr = self._mgr
        assert mgr is not None
        actions = mgr.get_available_actions()
        if any(a.get("action") in {"choose", "confirm_choice"} for a in actions):
            local = max(0, min(action - layout.combat_start, layout.combat_size - 1))
            self._note_choice_action(local)
            if local == 0:
                mgr.take_action({"action": "confirm_choice"})
            else:
                mgr.take_action({"action": "choose", "index": local - 1})
            return

        actions = [
            a for a in actions
            if a.get("action") == "event_choice"
        ]
        if not actions:
            return
        local = max(0, min(action - layout.event_start, len(actions) - 1))
        mgr.take_action(actions[local])

    def _step_noncombat_choice(self, action: int) -> None:
        mgr = self._mgr
        assert mgr is not None
        layout = _LAYOUT
        local = max(0, min(action - layout.combat_start, layout.combat_size - 1))
        self._note_choice_action(local)
        if local == 0:
            mgr.take_action({"action": "confirm_choice"})
        else:
            mgr.take_action({"action": "choose", "index": local - 1})

    def _step_treasure(self, local: int = 0) -> None:
        mgr = self._mgr
        assert mgr is not None
        if local == 1:
            # Only offered when the reward is actually skippable; fall back to
            # collecting rather than sending an action the manager will reject.
            actions = mgr.get_available_actions()
            if any(a.get("action") == "skip" for a in actions):
                mgr.take_action({"action": "skip"})
                return
        mgr.take_action({"action": "collect"})

    # ------------------------------------------------------------------
    # Shop helpers
    # ------------------------------------------------------------------

    def _get_shop_buy_actions(self) -> list[dict]:
        """Return the ordered list of buyable shop actions (excludes leave)."""
        mgr = self._mgr
        assert mgr is not None
        return [
            a for a in mgr.get_available_actions()
            if a.get("action") not in ("leave_shop",)
        ]

    def _mask_shop(self, actions: list[dict], mask: np.ndarray) -> None:
        """Populate *mask* for shop buy-actions at indices 1..N."""
        layout = _LAYOUT
        buyable = [
            a for a in actions if a.get("action") != "leave_shop"
        ]
        for i in range(min(len(buyable), layout.shop_size - 1)):
            mask[layout.shop_start + 1 + i] = 1

    # ------------------------------------------------------------------
    # Observation encoding
    # ------------------------------------------------------------------

    def _encode_obs(self) -> np.ndarray:
        """Encode the full run state as a flat float32 vector."""
        obs = np.zeros(RUN_OBS_SIZE, dtype=np.float32)

        mgr = self._mgr
        if mgr is None:
            return obs

        # ---- Combat observation (131 dims) ----
        combat = mgr.get_combat_state()
        if combat is not None:
            combat_obs = encode_observation(combat)
            n = min(len(combat_obs), COMBAT_OBS_SIZE)
            obs[:n] = combat_obs[:n]

        # ---- Run-level state (20 dims) ----
        rs = mgr.run_state
        player = rs.player
        idx = COMBAT_OBS_SIZE

        # Act / floor (3)
        obs[idx + 0] = rs.current_act_index / OBS_CURRENT_ACT_SCALE
        obs[idx + 1] = rs.total_floor / OBS_TOTAL_FLOOR_SCALE
        obs[idx + 2] = rs.act_floor / OBS_ACT_FLOOR_SCALE

        # HP ratio, gold (2)
        obs[idx + 3] = (
            player.current_hp / max(player.max_hp, 1)
        )
        obs[idx + 4] = player.gold / OBS_GOLD_SCALE

        # Deck size, relic count (2)
        obs[idx + 5] = len(player.deck) / OBS_DECK_SIZE_SCALE
        obs[idx + 6] = len(rs.relics) / OBS_RELIC_COUNT_SCALE

        # Potions (2)
        num_potions = sum(1 for p in player.potions if p is not None)
        max_slots = max(player.max_potion_slots, 1)
        obs[idx + 7] = num_potions / max_slots
        obs[idx + 8] = player.max_potion_slots / OBS_MAX_POTION_SLOTS_SCALE

        # Phase one-hot (8)
        phase_idx = _PHASE_INDEX.get(mgr.phase, 0)
        obs[idx + 9 + phase_idx] = 1.0

        # Ascension (1)
        obs[idx + 17] = rs.ascension_level / OBS_ASCENSION_SCALE

        # is_elite, is_boss flags (2)
        room = mgr._current_room_type
        obs[idx + 18] = 1.0 if room == RoomType.ELITE else 0.0
        obs[idx + 19] = 1.0 if room == RoomType.BOSS else 0.0

        np.clip(obs, OBS_VALUE_LOW, OBS_VALUE_HIGH, out=obs)
        return obs

    # ------------------------------------------------------------------
    # Info dict
    # ------------------------------------------------------------------

    def _build_info(self) -> dict[str, Any]:
        """Build the ``info`` dict returned by ``reset()`` and ``step()``."""
        info: dict[str, Any] = {"action_mask": self.action_masks()}
        if self._mgr is not None:
            rs = self._mgr.run_state
            info.update({
                "phase": self._mgr.phase,
                "act": rs.current_act_index,
                "floor": rs.total_floor,
                "hp": rs.player.current_hp,
                "max_hp": rs.player.max_hp,
                "gold": rs.player.gold,
                "deck_size": len(rs.player.deck),
                "relics": len(rs.relics),
                "step": self._step_count,
            })
        return info

    # ------------------------------------------------------------------
    # Render
    # ------------------------------------------------------------------

    def render(self) -> str | None:
        if self.render_mode != "ansi" or self._mgr is None:
            return None
        s = self._mgr.summary()
        lines = [
            "=== STS2 Run ===",
            f"Phase: {s['phase']}  |  Act {s['act']}  Floor {s['floor']}",
            f"HP: {s['hp']}  Gold: {s['gold']}  Deck: {s['deck_size']}",
            f"Relics: {s['relics']}",
            f"Step: {self._step_count}",
        ]
        combat = self._mgr.get_combat_state()
        if combat is not None:
            lines.append(str(combat))
        return "\n".join(lines)
