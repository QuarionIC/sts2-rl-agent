"""Rule-based Necrobinder heuristic agent (docs/TRAINING_REVAMP_SPEC Phase 6).

A competent -- deliberately NOT perfect -- scripted policy over the unified
``Discrete(157)`` run action space, used to generate BC bootstrap data
(``scripts/gen_bc_data.py``) and as a standalone baseline.

Decision rules
--------------
Combat:
  * play Souls eagerly (free draw, exhausts);
  * summon if Osty is dead and a summon card is in hand;
  * if the incoming attack damage is lethal (>= HP + block), play the best
    block card first;
  * otherwise play the best attack with Osty priority (Osty-attack cards are
    preferred while Osty lives and skipped while it is dead), targeting
    kills first, then the highest-threat enemy;
  * powers/utility fill leftover energy; end turn when nothing scores.
Drafting (card rewards / shop): prefers summon / Soul / Doom density
  (archetype sets derived in :mod:`sts2_env.gym_env.rich_observation`),
  skips weak picks once the deck is big enough; never picks curses.
Removal (shop / choice screens): removes curses, then Strikes, then Defends.
Rest: heal below 50% HP, else smith (upgrade priority mirrors draft scores).
Map: weak combats early, rest sites when hurt or late-act, elites avoided.
Events: keyword-scored safe defaults (avoid HP loss when hurt, take free
  upgrades/gold), otherwise the first option.
Choice screens: context-aware -- "remove/transform/exhaust/discard" prompts
  pick the WORST card, "upgrade/duplicate/copy" prompts pick the BEST card;
  unknown prompts confirm as soon as legal. Every returned action is checked
  against the env's action mask (the anti-dither guard stays authoritative);
  fall back to the first legal action.

Run standalone to measure the heuristic's win rate:

    python scripts/heuristic_agent.py --episodes 200 --max-act-count 1 \
        --ascension 0 --workers 8
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import time
from typing import Any

import numpy as np

from sts2_env.core.enums import CardId, CardTag, CardType, PowerId
from sts2_env.gym_env.rich_observation import (
    NECRO_DOOM_APPLIER_IDS,
    NECRO_SOUL_GENERATOR_IDS,
    NECRO_SOUL_PAYOFF_IDS,
    NECRO_SUMMON_CARD_IDS,
)
from sts2_env.gym_env.run_env import (
    _CARD_RWD_START,
    _EVENT_START,
    _MAP_START,
    _REST_START,
    _SHOP_START,
    _TREASURE_START,
)
from sts2_env.run.run_manager import RunManager

# Combat sub-layout (see sts2_env/core/constants.py):
#   0 = end turn / confirm choice
#   1 + i           = play hand slot i, untargeted   (i in 0..9)
#   11 + i*5 + j    = play hand slot i at enemy j    (j in 0..4)
_UNTARGETED_BASE = 1
_TARGETED_BASE = 11
_MAX_ENEMIES = 5
_MAX_HAND = 10

_BASIC_STRIKE_IDS = frozenset(
    cid for cid in CardId if cid.name.startswith("STRIKE_")
)
_BASIC_DEFEND_IDS = frozenset(
    cid for cid in CardId if cid.name.startswith("DEFEND_")
)

# Prompt keywords that mean "pick the WORST card" on a choice screen.
_WORST_PROMPT_WORDS = ("remove", "transform", "exhaust", "discard", "sacrifice")
# Prompt keywords that mean "pick the BEST card".
_BEST_PROMPT_WORDS = ("upgrade", "smith", "duplicate", "copy", "add", "keep")

_EVENT_POSITIVE = ("gain", "obtain", "heal", "upgrade", "free", "gold", "relic")
_EVENT_NEGATIVE = ("lose", "curse", "damage", "pay", "sacrifice", "give", "fight", "die")


def _card_draft_score(card_id_name: str, rarity: str | None = None,
                      upgraded: bool = False) -> float:
    """Archetype-density draft score for an offered card (by CardId name)."""
    try:
        cid = CardId[card_id_name]
    except KeyError:
        return 0.0
    score = 1.5
    if cid in NECRO_SUMMON_CARD_IDS:
        score += 3.5
    if cid in NECRO_SOUL_GENERATOR_IDS:
        score += 3.0
    if cid in NECRO_DOOM_APPLIER_IDS:
        score += 2.5
    if cid in NECRO_SOUL_PAYOFF_IDS:
        score += 2.5
    try:
        from sts2_env.cards.base import reference_canonical_tags

        if CardTag.OSTY_ATTACK in reference_canonical_tags(cid):
            score += 2.0
    except Exception:
        pass
    # Raw combat power: fast clears are the starter deck's missing piece.
    try:
        from sts2_env.cards.factory import create_card

        ref = create_card(cid)
        dmg = ref.base_damage or ref.effect_vars.get("damage", 0) or 0
        blk = ref.base_block or ref.effect_vars.get("block", 0) or 0
        if dmg >= 8:
            score += 1.0
        if blk >= 7:
            score += 0.6
        if dmg > 0 and ref.target_type.name == "ALL_ENEMIES":
            score += 0.5
    except Exception:
        pass
    if rarity == "RARE":
        score += 1.0
    elif rarity == "UNCOMMON":
        score += 0.5
    if upgraded:
        score += 0.5
    if cid in _BASIC_STRIKE_IDS or cid in _BASIC_DEFEND_IDS:
        score = 0.2
    if "CURSE" in (rarity or ""):
        score = -10.0
    return score


def _deck_card_badness(card) -> float:
    """Higher = better to remove/transform away."""
    if card.card_type == CardType.CURSE or card.is_status:
        return 10.0
    if card.card_id in _BASIC_STRIKE_IDS:
        return 8.0
    if card.card_id in _BASIC_DEFEND_IDS:
        return 6.0
    return max(0.0, 4.0 - _card_draft_score(card.card_id.name)) + (
        0.0 if card.upgraded else 0.5
    )


def _deck_card_upgrade_value(card) -> float:
    """Higher = better to upgrade."""
    if card.upgraded or card.card_type in (CardType.CURSE, CardType.STATUS):
        return -1.0
    return _card_draft_score(card.card_id.name)


class HeuristicNecrobinderAgent:
    """Rule-based policy over the unified run action space.

    ``act(env)`` returns a legal action index for ``env``'s current state
    (``env`` is an :class:`~sts2_env.gym_env.rich_run_env.RichSTS2RunEnv`
    or the base ``STS2RunEnv``; the agent reads the sim state white-box).
    """

    # ------------------------------------------------------------------

    def act(self, env) -> int:
        mask = np.asarray(env.action_masks(), dtype=bool)
        mgr = env._mgr
        if mgr is None or mgr.is_over:
            return self._first_legal(mask)

        phase = mgr.phase
        actions = mgr.get_available_actions()
        choice_like = phase != RunManager.PHASE_COMBAT and any(
            a.get("action") in {"choose", "confirm_choice"} for a in actions
        )
        try:
            if choice_like:
                action = self._choice_action(env, mask, actions)
            elif phase == RunManager.PHASE_COMBAT:
                action = self._combat_action(env, mgr, mask)
            elif phase == RunManager.PHASE_MAP_CHOICE:
                action = self._map_action(mgr, mask, actions)
            elif phase == RunManager.PHASE_CARD_REWARD:
                action = self._card_reward_action(mgr, mask, actions)
            elif phase == RunManager.PHASE_BOSS_RELIC:
                action = 127  # first boss relic
            elif phase == RunManager.PHASE_SHOP:
                action = self._shop_action(mgr, mask, actions)
            elif phase == RunManager.PHASE_REST_SITE:
                action = self._rest_action(mgr, mask, actions)
            elif phase == RunManager.PHASE_EVENT:
                action = self._event_action(mgr, mask, actions)
            elif phase == RunManager.PHASE_TREASURE:
                action = _TREASURE_START
            else:
                action = self._first_legal(mask)
        except Exception:
            action = self._first_legal(mask)

        if action is None or not (0 <= action < len(mask)) or not mask[action]:
            action = self._first_legal(mask)
        return int(action)

    # ------------------------------------------------------------------

    @staticmethod
    def _first_legal(mask: np.ndarray) -> int:
        legal = np.flatnonzero(mask)
        return int(legal[0]) if len(legal) else 0

    # ------------------------------------------------------------------
    # Combat
    # ------------------------------------------------------------------

    @staticmethod
    def _enemy_threat(combat, enemy) -> int:
        """Strength-adjusted damage this enemy intends to deal this turn."""
        if not enemy.is_alive:
            return 0
        ai = combat.enemy_ais.get(enemy.combat_id)
        move = ai.current_move if ai is not None else None
        if move is None:
            return 0
        try:
            strength = (enemy.get_power_amount(PowerId.STRENGTH)
                        + enemy.get_power_amount(PowerId.TEMPORARY_STRENGTH))
        except Exception:
            strength = 0
        threat = 0
        for intent in getattr(move, "intents", ()) or ():
            dmg = getattr(intent, "damage", 0) or 0
            if dmg > 0:
                per_hit = max(0, dmg + strength)
                threat += per_hit * max(1, getattr(intent, "hits", 1) or 1)
        return threat

    def _incoming_damage(self, combat) -> int:
        """Total strength/vulnerable-adjusted intent damage this turn."""
        total = sum(self._enemy_threat(combat, e) for e in combat.enemies)
        try:
            if combat.primary_player.get_power_amount(PowerId.VULNERABLE) > 0:
                total = int(total * 1.5)
        except Exception:
            pass
        return total

    @staticmethod
    def _card_damage_estimate(card, osty_alive: bool) -> float:
        dmg = card.effect_vars.get("osty_damage") if CardTag.OSTY_ATTACK in card.tags else None
        if dmg is None:
            dmg = card.base_damage or card.effect_vars.get("damage", 0) or 0
        hits = card.effect_vars.get("hits", 1) or 1
        return float(dmg) * max(1, hits)

    def _combat_action(self, env, mgr, mask: np.ndarray) -> int:
        combat = mgr.get_combat_state()
        if combat is None:
            return self._first_legal(mask)
        if combat.pending_choice is not None:
            return self._pending_choice_action(combat.pending_choice, mask)

        player = combat.primary_player
        hand = combat.hand
        osty = combat.get_osty(player)
        osty_alive = osty is not None and osty.is_alive
        osty_hp = (osty.current_hp + osty.block) if osty_alive else 0
        incoming = self._incoming_damage(combat)
        # Osty carries DIE_FOR_YOU: while it lives, attack damage aimed at
        # the player is redirected to Osty. Osty HP is therefore the real
        # front-line buffer; only damage past Osty (+player block) hits us.
        past_osty = max(0, incoming - osty_hp)
        unblocked = max(0, past_osty - player.block)
        lethal = past_osty >= player.current_hp + player.block

        enemies = combat.enemies
        alive = [e for e in enemies if e.is_alive]

        # Potions: drink when death is on the line, and spend them freely in
        # elite/boss fights (hoarding them past the act boss wastes them).
        room = getattr(mgr, "_current_room_type", None)
        room_name = getattr(room, "name", "")
        big_fight = room_name in ("ELITE", "BOSS")
        if lethal or (big_fight and combat.round_number >= 1 and alive):
            potion_actions = np.flatnonzero(mask[61:115])
            if len(potion_actions):
                return int(61 + potion_actions[0])

        best_action, best_score = None, 0.0
        for i, card in enumerate(hand[:_MAX_HAND]):
            candidates: list[tuple[int, float]] = []
            dmg = self._card_damage_estimate(card, osty_alive)
            untargeted = _UNTARGETED_BASE + i
            if untargeted < len(mask) and mask[untargeted]:
                bonus = 0.0
                if dmg > 0 and card.target_type.name == "ALL_ENEMIES" and any(
                    dmg >= e.current_hp + e.block for e in alive
                ):
                    bonus += 45.0  # AoE kill
                candidates.append((untargeted, bonus))
            for j in range(min(len(enemies), _MAX_ENEMIES)):
                a = _TARGETED_BASE + i * _MAX_ENEMIES + j
                if a < len(mask) and mask[a]:
                    enemy = enemies[j]
                    bonus = 0.0
                    threat = self._enemy_threat(combat, enemy)
                    # DOOM is an execute threshold: the enemy dies at end of
                    # turn once HP <= doom stacks, so it discounts kill HP.
                    try:
                        doom = enemy.get_power_amount(PowerId.DOOM)
                    except Exception:
                        doom = 0
                    kill_hp = enemy.current_hp + enemy.block - doom
                    if dmg > 0 and dmg >= kill_hp:
                        bonus += 45.0  # kill: removes the threat entirely
                        bonus += 0.3 * threat
                    # Focus fire: highest threat per remaining HP first.
                    bonus += 30.0 * threat / max(1.0, kill_hp + 5.0)
                    bonus -= 0.02 * enemy.current_hp  # finish low targets
                    candidates.append((a, bonus))
            if not candidates:
                continue

            score = self._combat_card_score(
                card, osty_alive, osty_hp, lethal, unblocked, incoming, len(alive))
            if score <= 0.0:
                continue
            action, target_bonus = max(candidates, key=lambda c: c[1])
            total = score + target_bonus
            if total > best_score:
                best_action, best_score = action, total

        if best_action is not None:
            return best_action
        return 0  # end turn

    def _combat_card_score(self, card, osty_alive: bool, osty_hp: int,
                           lethal: bool, unblocked: int, incoming: int,
                           n_alive: int) -> float:
        if card.is_curse or card.is_status:
            return 0.0
        cid = card.card_id
        is_osty_attack = CardTag.OSTY_ATTACK in card.tags
        block = card.base_block or card.effect_vars.get("block", 0) or 0
        dmg = self._card_damage_estimate(card, osty_alive)
        is_summon = cid in NECRO_SUMMON_CARD_IDS

        # Souls: free draw, always first.
        if cid == CardId.SOUL:
            return 100.0
        # Summon when Osty is down (rule from the spec): Osty redirects all
        # attack damage from the player to itself (DIE_FOR_YOU), so a dead
        # Osty means the player is the one soaking hits.
        if is_summon and not osty_alive:
            return 90.0
        # Osty attacks whiff while Osty is dead.
        if is_osty_attack and not osty_alive:
            return 0.0

        # Osty is the real HP buffer: pump it whenever this turn's incoming
        # would chew through it (summoning is persistent damage prevention).
        if is_summon and osty_alive:
            deficit = incoming - osty_hp
            if deficit > 0:
                return 55.0 + min(20.0, deficit)
            if osty_hp <= 2:
                return 34.0

        if lethal and block > 0:
            # Survival first (kill bonuses can still outrank via +45).
            return 70.0 + block

        # Player block only matters for damage that gets PAST Osty.
        if block > 0 and unblocked > 0:
            overblock = max(0, block - unblocked - 4)
            return 42.0 + min(block, unblocked) - 0.5 * overblock

        if dmg > 0:
            score = 30.0 + 0.5 * dmg + (8.0 if is_osty_attack else 0.0)
            if n_alive > 1 and card.target_type.name == "ALL_ENEMIES":
                score += 0.4 * dmg * (n_alive - 1)
            return score
        if card.is_power:
            return 26.0
        if cid in NECRO_SOUL_GENERATOR_IDS or is_summon:
            return 24.0
        if block > 0:
            return 3.0  # nothing reaches the player: blocking is wasted
        return 4.0  # misc utility skill

    # ------------------------------------------------------------------
    # Choice screens (combat + run-level + event)
    # ------------------------------------------------------------------

    def _pending_choice_action(self, choice, mask: np.ndarray) -> int:
        prompt = (getattr(choice, "prompt", "") or "").lower()
        options = getattr(choice, "options", []) or []
        selected = set(getattr(choice, "selected_indices", ()) or ())
        can_confirm = bool(mask[0])

        want_worst = any(w in prompt for w in _WORST_PROMPT_WORDS)
        want_best = any(w in prompt for w in _BEST_PROMPT_WORDS)

        if selected and can_confirm:
            return 0  # something already selected: lock it in

        scored: list[tuple[float, int]] = []
        for i, option in enumerate(options):
            a = 1 + i
            if a >= len(mask) or not mask[a] or i in selected:
                continue
            card = getattr(option, "card", None)
            if card is None:
                scored.append((0.0, a))
                continue
            if want_worst:
                scored.append((_deck_card_badness(card), a))
            elif want_best:
                scored.append((_deck_card_upgrade_value(card), a))
            else:
                scored.append((_card_draft_score(card.card_id.name), a))
        if scored:
            score, action = max(scored, key=lambda s: s[0])
            # On unknown prompts, prefer confirming over toggling junk.
            if can_confirm and not want_worst and not want_best and score <= 0.5:
                return 0
            return action
        if can_confirm:
            return 0
        return self._first_legal(mask)

    def _choice_action(self, env, mask: np.ndarray, actions: list[dict]) -> int:
        choice = env._active_choice_object()
        if choice is not None:
            return self._pending_choice_action(choice, mask)
        return self._first_legal(mask)

    # ------------------------------------------------------------------
    # Map / rewards / shop / rest / event
    # ------------------------------------------------------------------

    def _map_action(self, mgr, mask: np.ndarray, actions: list[dict]) -> int:
        rs = mgr.run_state
        hp_frac = rs.player.current_hp / max(1, rs.player.max_hp)
        late = rs.act_floor >= 8
        prefs: dict[str, float] = {
            # Weak combats early for card rewards; when hurt, prefer the
            # no-fight rooms (events, treasure, rest) to stop the bleed.
            "MONSTER": 5.0 if hp_frac >= 0.8 else 3.0,
            "UNKNOWN": 4.5 if hp_frac >= 0.8 else 5.0,
            "TREASURE": 5.5,
            "SHOP": 2.5 if rs.player.gold >= 150 else 1.5,
            "REST_SITE": 8.0 if (hp_frac < 0.7 or (late and hp_frac < 0.9)) else 2.0,
            "ELITE": 0.2,
            "BOSS": 5.0,
            "ANCIENT": 2.0,
            "UNASSIGNED": 1.0,
        }
        moves = [a for a in actions if a.get("action") == "move"]
        # 2-row lookahead: a node is worth its own score plus a discounted
        # best-child score, so the route bends toward upcoming rests and
        # away from elite-heavy columns (entering the boss hurt is the top
        # scripted-run killer).
        coords = list(getattr(mgr, "_available_coords", []) or [])
        act_map = rs.map
        best, best_score = None, -1e9
        for i, move in enumerate(moves[:5]):
            a = _MAP_START + i
            if a >= len(mask) or not mask[a]:
                continue
            score = prefs.get(move.get("point_type", "UNKNOWN"), 1.0)
            try:
                point = act_map.get_point(coords[i]) if (act_map and i < len(coords)) else None
                if point is not None and point.children:
                    child_best = max(
                        prefs.get(child.point_type.name, 1.0)
                        for child in point.children
                    )
                    score += 0.5 * child_best
            except Exception:
                pass
            if score > best_score:
                best, best_score = a, score
        return best if best is not None else self._first_legal(mask)

    def _card_reward_action(self, mgr, mask: np.ndarray, actions: list[dict]) -> int:
        # Potion / relic offers ride the same phase: local 0 = take.
        if any(a.get("action") in {"pick_potion", "pick_relic_reward"} for a in actions):
            return _CARD_RWD_START  # always take free potions/relics
        picks = [a for a in actions if a.get("action") == "pick_card"]
        deck_size = len(mgr.run_state.player.deck)
        best_local, best_score = None, -1e9
        for a in picks:
            idx = a.get("index", 0)
            env_action = _CARD_RWD_START + idx if idx < 3 else 124 + (idx - 3)
            if env_action >= len(mask) or not mask[env_action]:
                continue
            score = _card_draft_score(
                a.get("card_id", ""), a.get("rarity"), bool(a.get("upgraded")))
            if score > best_score:
                best_local, best_score = env_action, score
        # Skip weak picks once the deck has substance (density > size).
        threshold = 1.0 if deck_size < 18 else 2.5
        if best_local is not None and best_score >= threshold:
            return best_local
        skip = _CARD_RWD_START + 3
        if skip < len(mask) and mask[skip]:
            return skip
        return best_local if best_local is not None else self._first_legal(mask)

    def _shop_action(self, mgr, mask: np.ndarray, actions: list[dict]) -> int:
        buyable = [a for a in actions if a.get("action") != "leave_shop"]
        best_i, best_score = None, 0.0
        for i, a in enumerate(buyable[:9]):
            env_action = _SHOP_START + 1 + i
            if env_action >= len(mask) or not mask[env_action]:
                continue
            kind = a.get("action")
            if kind == "remove_card":
                score = 6.0  # remove a Strike/curse: the best gold sink
            elif kind == "buy_card":
                score = _card_draft_score(
                    a.get("card_id", ""), a.get("rarity"), bool(a.get("upgraded")))
                score = score - 2.0  # only buy clearly good cards
            elif kind == "buy_relic":
                score = 3.0
            elif kind == "buy_potion":
                score = 1.0
            else:
                score = 0.0
            if score > best_score:
                best_i, best_score = env_action, score
        if best_i is not None and best_score > 2.0:
            return best_i
        return _SHOP_START  # leave

    def _rest_action(self, mgr, mask: np.ndarray, actions: list[dict]) -> int:
        rs = mgr.run_state
        hp_frac = rs.player.current_hp / max(1, rs.player.max_hp)
        rest_actions = [a for a in actions if a.get("action") == "rest_option"]
        late = mgr.run_state.act_floor >= 8
        want = "HEAL" if hp_frac < (0.65 if late else 0.6) else "SMITH"
        order = [want] + (["SMITH", "HEAL"] if want == "HEAL" else ["HEAL"])
        for target in order:
            for i, a in enumerate(rest_actions[:5]):
                env_action = _REST_START + i
                if a.get("option_id") == target and env_action < len(mask) and mask[env_action]:
                    return env_action
        return self._first_legal(mask)

    def _event_action(self, mgr, mask: np.ndarray, actions: list[dict]) -> int:
        rs = mgr.run_state
        hp_frac = rs.player.current_hp / max(1, rs.player.max_hp)
        options = [a for a in actions if a.get("action") == "event_choice"]
        best, best_score = None, -1e9
        for i, a in enumerate(options[:4]):
            env_action = _EVENT_START + i
            if env_action >= len(mask) or not mask[env_action]:
                continue
            text = f"{a.get('label', '')} {a.get('description', '')}".lower()
            score = 0.0
            for w in _EVENT_POSITIVE:
                if w in text:
                    score += 2.0
            for w in _EVENT_NEGATIVE:
                if w in text:
                    score -= 2.0 if hp_frac > 0.5 else 4.0
            if a.get("option_id") == "leave":
                score += 0.5  # mild safety bias
            if score > best_score:
                best, best_score = env_action, score
        return best if best is not None else self._first_legal(mask)


# ---------------------------------------------------------------------------
# Standalone evaluation
# ---------------------------------------------------------------------------

def play_episode(agent: HeuristicNecrobinderAgent, env, seed: int,
                 max_steps: int = 3000) -> dict[str, Any]:
    obs, info = env.reset(seed=seed)
    done = False
    steps = 0
    while not done and steps < max_steps:
        action = agent.act(env)
        obs, reward, terminated, truncated, info = env.step(action)
        done = terminated or truncated
        steps += 1
    return {
        "won": bool(info.get("won", False)),
        "floor": int(info.get("floor", 0)),
        "act": int(info.get("act", 0)),
        "steps": steps,
        "truncated": bool(info.get("truncated", False)),
    }


def _worker_eval(args: tuple[int, int, int, int]) -> list[dict[str, Any]]:
    """Play a block of episodes in one worker process (torch-free)."""
    seed_start, n_episodes, ascension, max_act_count = args
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        max_act_count=max_act_count,
        reward_config=RewardConfig(shaping_scale=0.0),
    )
    agent = HeuristicNecrobinderAgent()
    return [play_episode(agent, env, seed_start + ep) for ep in range(n_episodes)]


def evaluate(n_episodes: int = 200, ascension: int = 0, max_act_count: int = 1,
             workers: int = 1, seed_block: int = 20_000_000) -> dict[str, Any]:
    start = time.perf_counter()
    if workers <= 1:
        results = _worker_eval((seed_block, n_episodes, ascension, max_act_count))
    else:
        import multiprocessing as mp

        per = [n_episodes // workers] * workers
        for i in range(n_episodes % workers):
            per[i] += 1
        jobs = []
        offset = 0
        for n in per:
            jobs.append((seed_block + offset, n, ascension, max_act_count))
            offset += n
        ctx = mp.get_context("spawn")
        with ctx.Pool(workers) as pool:
            results = [r for block in pool.map(_worker_eval, jobs) for r in block]

    wins = sum(r["won"] for r in results)
    deaths_by_act: dict[int, int] = {}
    for r in results:
        if not r["won"]:
            deaths_by_act[r["act"]] = deaths_by_act.get(r["act"], 0) + 1
    return {
        "episodes": len(results),
        "win_rate": wins / max(1, len(results)),
        "mean_floors": float(np.mean([r["floor"] for r in results])),
        "mean_steps": float(np.mean([r["steps"] for r in results])),
        "truncation_rate": float(np.mean([r["truncated"] for r in results])),
        "deaths_by_act": deaths_by_act,
        "wall_s": round(time.perf_counter() - start, 1),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Evaluate the Necrobinder heuristic")
    parser.add_argument("--episodes", type=int, default=200)
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--max-act-count", type=int, default=1)
    parser.add_argument("--workers", type=int, default=1)
    parser.add_argument("--seed-block", type=int, default=20_000_000)
    args = parser.parse_args()

    metrics = evaluate(args.episodes, args.ascension, args.max_act_count,
                       args.workers, args.seed_block)
    print(f"heuristic eval (A{args.ascension}, acts 1-{args.max_act_count}): "
          f"win_rate={metrics['win_rate']:.1%} over {metrics['episodes']} episodes | "
          f"mean_floors={metrics['mean_floors']:.1f} "
          f"mean_steps={metrics['mean_steps']:.0f} "
          f"trunc={metrics['truncation_rate']:.1%} "
          f"deaths_by_act={metrics['deaths_by_act']} ({metrics['wall_s']}s)")


if __name__ == "__main__":
    main()
