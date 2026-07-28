"""Knowledge-driven run policy: skills -> concrete env actions.

Bridges :mod:`sts2_env.knowledge.skills` (option-space decisions) to the
unified 157-action space of ``HierarchicalRunEnv``. Resolution goes through
``RunManager.get_available_actions()`` and matches on the action DICT rather
than on slice arithmetic, because the card-reward slice is only 4 wide and
pending run choices reuse the combat slice -- an offset-based mapping gave
wrong answers earlier in this project and silently mislabelled 209 "picks"
that were really skips.

Any phase without a skill falls back to the env's own first legal action, so
the policy is always complete and never illegal.
"""

from __future__ import annotations

from collections import Counter
from typing import Any

import numpy as np

from sts2_env.knowledge.skills import (
    skill_pick_card,
    skill_rest_or_smith,
    skill_route,
    skill_shop,
    skill_smith_target,
)
from sts2_env.run.run_manager import RunManager


class KnowledgeRunPolicy:
    """Scripted out-of-combat policy built from community priors.

    Serves three roles: a strong non-learned baseline, a verification target
    for individual skills (toggle them off one at a time), and a source of
    demonstrations for warm-starting the RL run agent.
    """

    def __init__(self, env, enabled: set[str] | None = None):
        self.env = env
        #: Which skills are active; used for ablating one skill at a time.
        self.enabled = enabled if enabled is not None else {
            "pick_card", "rest_or_smith", "smith_target", "shop", "route"
        }
        self.decisions = Counter()
        self.skill_fired = Counter()

    # -- helpers --------------------------------------------------------

    def _deck_context(self) -> tuple[Counter, int]:
        rs = self.env._mgr.run_state
        deck = rs.player.deck
        counter = Counter(str(c.card_id).replace("CardId.", "") for c in deck)
        return counter, len(deck)

    def _hp_frac(self) -> float:
        p = self.env._mgr.run_state.player
        return float(p.current_hp) / float(max(p.max_hp, 1))

    def _action_index(self, target: dict, actions: list[dict],
                      mask: np.ndarray) -> int | None:
        """Find the env action index whose resolved action dict is ``target``.

        Probes candidate indices by asking the manager what each would do.
        Uses the phase's declared slice as the search space but VERIFIES by
        dict identity, so a wrong offset can never silently pick a different
        action.
        """
        from sts2_env.gym_env.run_env import _LAYOUT

        mgr = self.env._mgr
        starts = {
            RunManager.PHASE_MAP_CHOICE: (_LAYOUT.map_start, _LAYOUT.map_size),
            RunManager.PHASE_CARD_REWARD: (_LAYOUT.card_reward_start,
                                           _LAYOUT.card_reward_size),
            RunManager.PHASE_SHOP: (_LAYOUT.shop_start, _LAYOUT.shop_size),
            RunManager.PHASE_REST_SITE: (_LAYOUT.rest_start, _LAYOUT.rest_size),
            RunManager.PHASE_EVENT: (_LAYOUT.event_start, _LAYOUT.event_size),
            RunManager.PHASE_TREASURE: (_LAYOUT.treasure_start,
                                        _LAYOUT.treasure_size),
        }
        span = starts.get(mgr.phase)
        if span is None:
            return None
        base, size = span
        try:
            local = actions.index(target)
        except ValueError:
            return None
        idx = base + local
        if 0 <= idx < mask.size and mask[idx] and local < size:
            return idx
        return None

    # -- main -----------------------------------------------------------

    def _pending_choice_action(self, actions: list[dict],
                               mask: np.ndarray) -> int | None:
        """Resolve a pending card choice toward the highest-prior card.

        The choice slice is the combat slice: index 0 is confirm, and option
        i is at 1+i (mirrors run_env's dispatch for non-combat choices).
        """
        from sts2_env.gym_env.run_env import _LAYOUT
        from sts2_env.knowledge.card_priors import card_prior

        choices = [a for a in actions if a.get("action") == "choose"]
        if not choices:
            return None
        best_local = None
        best_val = -1.0
        for i, a in enumerate(choices):
            cid = str(a.get("card_id", "") or a.get("option_id", ""))
            val = card_prior(cid).value if cid else 0.0
            if val > best_val:
                best_val, best_local = val, i
        if best_local is None:
            return None
        idx = _LAYOUT.combat_start + 1 + best_local
        if 0 <= idx < mask.size and mask[idx]:
            return idx
        return None

    def act(self, obs, mask) -> int:
        mask = np.asarray(mask, dtype=bool)
        mgr = self.env._mgr
        legal = np.flatnonzero(mask)
        if not legal.size:
            return 0
        if mgr is None:
            return int(legal[0])

        phase = mgr.phase
        actions = mgr.get_available_actions()
        self.decisions[phase] += 1
        chosen: dict | None = None

        # A pending run choice (e.g. WHICH card to upgrade after SMITH)
        # reuses the combat slice and is not a phase of its own. Without
        # this branch the upgrade landed on an arbitrary card, which is
        # most often a Strike -- the smith_target skill existed but was
        # never reachable.
        if mgr.run_state.pending_choice is not None and "smith_target" in self.enabled:
            idx = self._pending_choice_action(actions, mask)
            if idx is not None:
                self.skill_fired["smith_target"] += 1
                return idx

        if phase == RunManager.PHASE_CARD_REWARD and "pick_card" in self.enabled:
            picks = [a for a in actions if a.get("action") == "pick_card"]
            if picks:
                counter, size = self._deck_context()
                ctx = {
                    "card_options": [str(a.get("card_id", "")) for a in picks],
                    "deck_counter": counter,
                    "deck_size": size,
                    "floor": mgr.run_state.total_floor,
                    "can_skip": any(a.get("action") == "skip" for a in actions),
                }
                choice = skill_pick_card(ctx)
                if choice is None:
                    chosen = next((a for a in actions
                                   if a.get("action") == "skip"), None)
                else:
                    chosen = picks[choice]
                self.skill_fired["pick_card"] += 1

        elif phase == RunManager.PHASE_REST_SITE and "rest_or_smith" in self.enabled:
            opts = [a for a in actions if a.get("action") == "rest_option"]
            ids = {str(a.get("option_id")): a for a in opts}
            if ids:
                want = skill_rest_or_smith({
                    "hp_frac": self._hp_frac(),
                    "has_upgradable": "SMITH" in ids,
                })
                chosen = ids.get(want) or next(iter(ids.values()))
                self.skill_fired["rest_or_smith"] += 1

        elif phase == RunManager.PHASE_MAP_CHOICE and "route" in self.enabled:
            moves = [a for a in actions if a.get("action") == "move"]
            if moves:
                ctx = {
                    "route_options": [(i, str(a.get("point_type", "")))
                                      for i, a in enumerate(moves)],
                    "hp_frac": self._hp_frac(),
                    "gold": mgr.run_state.player.gold,
                }
                choice = skill_route(ctx)
                if choice is not None:
                    chosen = moves[choice]
                self.skill_fired["route"] += 1

        elif phase == RunManager.PHASE_SHOP and "shop" in self.enabled:
            buys = [(i, str(a.get("card_id", "")), int(a.get("price", 10**9)))
                    for i, a in enumerate(actions)
                    if a.get("action") == "buy_card"]
            removals = [a for a in actions if a.get("action") == "remove_card"]
            counter, size = self._deck_context()
            ctx = {
                "gold": mgr.run_state.player.gold,
                "deck_size": size,
                "removal_cost": (int(removals[0].get("price", 0))
                                 if removals else None),
                "shop_cards": buys,
            }
            decision = skill_shop(ctx)
            if decision is not None:
                kind, idx = decision
                if kind == "remove" and removals:
                    chosen = removals[0]
                elif kind == "buy_card":
                    chosen = next((a for i, a in enumerate(actions)
                                   if i == idx), None)
            if chosen is None:
                chosen = next((a for a in actions
                               if a.get("action") == "leave_shop"), None)
            self.skill_fired["shop"] += 1

        if chosen is not None:
            idx = self._action_index(chosen, actions, mask)
            if idx is not None:
                return idx

        # No skill applies (events, choices, treasure) or resolution failed:
        # fall back to a legal action rather than guessing.
        return int(legal[0])


def smith_choice_index(env, actions: list[dict]) -> int | None:
    """Pick which card to upgrade when a SMITH sub-choice is presented."""
    rs = env._mgr.run_state
    upgradable = [
        (i, str(c.card_id).replace("CardId.", ""))
        for i, c in enumerate(rs.player.deck) if not c.upgraded
    ]
    return skill_smith_target({"upgradable": upgradable})
