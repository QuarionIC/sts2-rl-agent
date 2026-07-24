"""Rich observation encoding (v1) for the Necrobinder A10 training redesign.

Implements the "New observation" section of docs/TRAINING_REDESIGN.md as a
single flat float32 vector shared by BOTH the combat-only env
(:class:`~sts2_env.gym_env.rich_combat_env.RichSTS2CombatEnv`) and the
full-run env (:class:`~sts2_env.gym_env.rich_run_env.RichSTS2RunEnv`).

The layout is deliberately identical across both envs so that policy weights
transfer between curriculum stages: the combat env zeroes the run-level
segment; the run env zeroes the combat segments while out of combat.

Layout (``RICH_OBS_SIZE`` dims total, see the segment table below).  All
embedding-index dims (integer-valued: hand card ids, potion ids, boss id)
live in one contiguous block at the very start of the vector so the policy's
feature extractor can slice + embed them.

======================  ==============================  =====
Segment                 Offset constant                 Size
======================  ==============================  =====
Hand card ids (int)     ``IDS_HAND_OFF``                10
Potion ids (int)        ``IDS_POTION_OFF``              5
Boss id (int)           ``IDS_BOSS_OFF``                1
Hand scalars            ``HAND_SCALARS_OFF``            10*12
Pile bags               ``PILE_BAGS_OFF``               3*582
Pile sizes              ``PILE_SIZES_OFF``              3
Player core             ``PLAYER_CORE_OFF``             10
Player powers           ``PLAYER_POWERS_OFF``           282
Necrobinder state       ``NECRO_OFF``                   17
Enemies                 ``ENEMIES_OFF``                 5*304
Relics                  ``RELICS_OFF``                  296
Potion usable flags     ``POTION_FLAGS_OFF``            5
Run-level state         ``RUN_OFF``                     78
======================  ==============================  =====

Card ids are encoded as ``list(CardId).index(card_id) + 1`` (0 = empty slot).
Potion ids are ``sorted(all potion model ids).index(pid) + 1`` (0 = empty).
The boss id indexes a vocabulary built dynamically from the act-slot
candidate registry (:mod:`sts2_env.map.acts`) plus every importable
encounter module's ``BOSS_ENCOUNTERS`` (0 = unknown).
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

import numpy as np

from sts2_env.core.combat import CombatState
from sts2_env.core.constants import MAX_ENEMIES, MAX_HAND_SIZE
from sts2_env.core.enums import CardId, CardType, IntentType, MapPointType, PowerId, RoomType
from sts2_env.map.acts import ACT_3, NUM_ACT_SLOTS, act_candidates_for_slot
from sts2_env.potions.base import all_potion_models
from sts2_env.relics.base import RelicId

if TYPE_CHECKING:
    from sts2_env.run.run_manager import RunManager

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------

RICH_OBS_VERSION = 1

# ---------------------------------------------------------------------------
# Vocabularies (id -> embedding index maps)
# ---------------------------------------------------------------------------

CARD_IDS: list[CardId] = list(CardId)
NUM_CARD_IDS = len(CARD_IDS)  # 582 at time of writing
CARD_ID_TO_IDX: dict[CardId, int] = {cid: i for i, cid in enumerate(CARD_IDS)}

POWER_IDS: list[PowerId] = list(PowerId)
NUM_POWER_IDS = len(POWER_IDS)  # 282 at time of writing
POWER_ID_TO_IDX: dict[PowerId, int] = {pid: i for i, pid in enumerate(POWER_IDS)}

RELIC_IDS: list[RelicId] = list(RelicId)
NUM_RELIC_IDS = len(RELIC_IDS)  # 295 at time of writing
RELIC_NAME_TO_IDX: dict[str, int] = {rid.name: i for i, rid in enumerate(RELIC_IDS)}

POTION_IDS: list[str] = sorted({m.potion_id for m in all_potion_models()})
NUM_POTION_IDS = len(POTION_IDS)
POTION_ID_TO_IDX: dict[str, int] = {pid: i for i, pid in enumerate(POTION_IDS)}
# Padded vocab size for the policy embedding table (leaves slack for potions
# registered later by mod content without changing the obs layout).
POTION_VOCAB_SIZE = 96

INTENT_TYPES: list[IntentType] = list(IntentType)
NUM_INTENT_TYPES = len(INTENT_TYPES)  # 15
INTENT_TO_IDX: dict[IntentType, int] = {it: i for i, it in enumerate(INTENT_TYPES)}

MAP_POINT_TYPES: list[MapPointType] = list(MapPointType)
NUM_MAP_POINT_TYPES = len(MAP_POINT_TYPES)  # 9
MAP_POINT_TO_IDX: dict[MapPointType, int] = {mt: i for i, mt in enumerate(MAP_POINT_TYPES)}


def _build_boss_vocab() -> dict[str, int]:
    """Boss-identity vocabulary, read dynamically so it reflects the act-slot
    candidate registry (legacy acts appear automatically once registered).

    Contains both the ActConfig ``boss_ids`` strings (e.g. ``"TheLich"``) and
    the encounter-setup function names (e.g. ``"setup_vantom_boss"``) since
    RunManager rolls the boss as a setup callable.
    """
    names: set[str] = set()
    for slot in range(NUM_ACT_SLOTS):
        for act in act_candidates_for_slot(slot):
            names.update(act.boss_ids)
    names.update(ACT_3.boss_ids)

    module_names = (
        "sts2_env.encounters.act1",
        "sts2_env.encounters.act2",
        "sts2_env.encounters.act3",
        "sts2_env.encounters.act4",
        "sts2_env.encounters.act4_heart",
        "sts2_env.encounters.exordium",
        "sts2_env.encounters.thecity",
        "sts2_env.encounters.thebeyond",
    )
    import importlib

    for mod_name in module_names:
        try:
            mod = importlib.import_module(mod_name)
        except ImportError:
            continue
        for setup in getattr(mod, "BOSS_ENCOUNTERS", []):
            name = getattr(setup, "__name__", None)
            if name:
                names.add(name)
    # Index 0 is reserved for "unknown boss".
    return {name: i + 1 for i, name in enumerate(sorted(names))}


BOSS_NAME_TO_IDX: dict[str, int] = _build_boss_vocab()
# Padded vocab size for the policy embedding table (slack for TheBeyond etc.).
BOSS_VOCAB_SIZE = 64
assert len(BOSS_NAME_TO_IDX) + 1 <= BOSS_VOCAB_SIZE, "boss vocab overflow"

# ---------------------------------------------------------------------------
# Segment geometry -- every offset/size is a module constant.
# ---------------------------------------------------------------------------

# --- embedding-ID block (contiguous, at the very start) ---
NUM_HAND_SLOTS = MAX_HAND_SIZE            # 10
NUM_POTION_SLOTS = 5                      # observed potion slots
IDS_HAND_OFF = 0
IDS_HAND_SIZE = NUM_HAND_SLOTS
IDS_POTION_OFF = IDS_HAND_OFF + IDS_HAND_SIZE
IDS_POTION_SIZE = NUM_POTION_SLOTS
IDS_BOSS_OFF = IDS_POTION_OFF + IDS_POTION_SIZE
IDS_BOSS_SIZE = 1
ID_BLOCK_SIZE = IDS_BOSS_OFF + IDS_BOSS_SIZE  # 16

# --- hand scalar features: per-slot, aligned with IDS_HAND slots ---
HAND_FEATURES = 12
# per-slot feature order:
#  0 cost_effective/5   1 star_cost/5   2 upgraded        3 playable
#  4 base_damage/50     5 base_block/50 6 is_attack       7 is_skill
#  8 is_power           9 ethereal     10 exhausts       11 is_curse_or_status
HAND_SCALARS_OFF = ID_BLOCK_SIZE
HAND_SCALARS_SIZE = NUM_HAND_SLOTS * HAND_FEATURES  # 120

# --- bag-of-cards count vectors over the full CardId space ---
NUM_PILES = 3  # draw, discard, exhaust
PILE_BAGS_OFF = HAND_SCALARS_OFF + HAND_SCALARS_SIZE
PILE_BAG_SIZE = NUM_CARD_IDS
PILE_BAGS_SIZE = NUM_PILES * PILE_BAG_SIZE  # 1746
BAG_COUNT_SCALE = 5.0

PILE_SIZES_OFF = PILE_BAGS_OFF + PILE_BAGS_SIZE
PILE_SIZES_SIZE = NUM_PILES  # 3

# --- player combat core ---
PLAYER_CORE_OFF = PILE_SIZES_OFF + PILE_SIZES_SIZE
PLAYER_CORE_SIZE = 10
# order: hp_ratio, max_hp/100, block/50, energy/10, max_energy/10,
#        stars/10, round/50, in_combat, pending_choice, num_choice_options/10

# --- full power amounts for the player ---
PLAYER_POWERS_OFF = PLAYER_CORE_OFF + PLAYER_CORE_SIZE
PLAYER_POWERS_SIZE = NUM_POWER_IDS  # 282
POWER_AMOUNT_SCALE = 20.0

# --- Necrobinder specifics ---
NECRO_OFF = PLAYER_POWERS_OFF + PLAYER_POWERS_SIZE
NUM_ALLY_SLOTS = 3
NECRO_OSTY_SIZE = 4    # alive, hp_ratio, max_hp/50, block/50
NECRO_SOULS_SIZE = 4   # SOUL cards in hand/draw/discard/exhaust (/10)
NECRO_ALLY_FEATURES = 3  # alive, hp_ratio, block/50 (per ally slot)
NECRO_SIZE = NECRO_OSTY_SIZE + NECRO_SOULS_SIZE + NUM_ALLY_SLOTS * NECRO_ALLY_FEATURES  # 17

# --- enemies ---
ENEMY_CORE_FEATURES = 7
# order: alive, hp_ratio, max_hp/250, block/50, intent_dmg/30, intent_hits/5, reserved
ENEMY_BLOCK_SIZE = ENEMY_CORE_FEATURES + NUM_INTENT_TYPES + NUM_POWER_IDS  # 304
ENEMIES_OFF = NECRO_OFF + NECRO_SIZE
ENEMIES_SIZE = MAX_ENEMIES * ENEMY_BLOCK_SIZE  # 1520

# --- relics: binary vector over RelicId space + normalized count ---
RELICS_OFF = ENEMIES_OFF + ENEMIES_SIZE
RELICS_SIZE = NUM_RELIC_IDS + 1  # 296

# --- potion usable flags, aligned with IDS_POTION slots ---
POTION_FLAGS_OFF = RELICS_OFF + RELICS_SIZE
POTION_FLAGS_SIZE = NUM_POTION_SLOTS  # 5

# --- run-level state ---
RUN_OFF = POTION_FLAGS_OFF + POTION_FLAGS_SIZE
NUM_ACTS_ONEHOT = 4
MAX_ACT_CANDIDATES = 4  # one-hot width per act slot for candidate selection
MAP_LOOKAHEAD_ROWS = 3
NUM_RUN_PHASES = 9  # 8 interactive phases + RUN_OVER
DECK_AGG_SIZE = 7
# run sub-segment order (relative to RUN_OFF):
RUN_ACT_OFF = 0                       # act_norm(1) + act one-hot(4)         = 5
RUN_FLOOR_OFF = RUN_ACT_OFF + 1 + NUM_ACTS_ONEHOT       # act_floor, total_floor = 2
RUN_HP_GOLD_OFF = RUN_FLOOR_OFF + 2   # hp_ratio, max_hp/100, gold/1000      = 3
RUN_KEYS_OFF = RUN_HP_GOLD_OFF + 3    # emerald, ruby, sapphire, count/3     = 4
RUN_DECK_OFF = RUN_KEYS_OFF + 4       # deck aggregates                      = 7
RUN_ACT_CAND_OFF = RUN_DECK_OFF + DECK_AGG_SIZE  # 3 slots x (onehot4+legacy) = 15
RUN_LOOKAHEAD_OFF = RUN_ACT_CAND_OFF + NUM_ACT_SLOTS * (MAX_ACT_CANDIDATES + 1)  # 3x9 = 27
RUN_PHASE_OFF = RUN_LOOKAHEAD_OFF + MAP_LOOKAHEAD_ROWS * NUM_MAP_POINT_TYPES
# phase one-hot(9) + subscreen flags: offered_potion, offered_relic, run_pending_choice = 12
RUN_MISC_OFF = RUN_PHASE_OFF + NUM_RUN_PHASES + 3  # ascension/20, is_elite, is_boss = 3
RUN_SIZE = RUN_MISC_OFF + 3  # 78

RICH_OBS_SIZE = RUN_OFF + RUN_SIZE

# Value bounds for the Box space.
RICH_OBS_LOW = -1.0
# The ID block holds raw integer indices (up to NUM_CARD_IDS), so the Box
# high bound must accommodate them.
RICH_OBS_HIGH = float(max(NUM_CARD_IDS, NUM_POTION_IDS, BOSS_VOCAB_SIZE) + 1)

# Phase one-hot index (run env).
RUN_PHASE_INDEX: dict[str, int] = {
    "MAP_CHOICE": 0,
    "COMBAT": 1,
    "CARD_REWARD": 2,
    "BOSS_RELIC": 3,
    "SHOP": 4,
    "REST_SITE": 5,
    "EVENT": 6,
    "TREASURE": 7,
    "RUN_OVER": 8,
}

_PILE_ATTRS = ("draw_pile", "discard_pile", "exhaust_pile")


def segment_table() -> list[tuple[str, int, int]]:
    """Human-readable (name, offset, size) table -- used by tests/docs."""
    return [
        ("ids_hand", IDS_HAND_OFF, IDS_HAND_SIZE),
        ("ids_potion", IDS_POTION_OFF, IDS_POTION_SIZE),
        ("ids_boss", IDS_BOSS_OFF, IDS_BOSS_SIZE),
        ("hand_scalars", HAND_SCALARS_OFF, HAND_SCALARS_SIZE),
        ("pile_bags", PILE_BAGS_OFF, PILE_BAGS_SIZE),
        ("pile_sizes", PILE_SIZES_OFF, PILE_SIZES_SIZE),
        ("player_core", PLAYER_CORE_OFF, PLAYER_CORE_SIZE),
        ("player_powers", PLAYER_POWERS_OFF, PLAYER_POWERS_SIZE),
        ("necro", NECRO_OFF, NECRO_SIZE),
        ("enemies", ENEMIES_OFF, ENEMIES_SIZE),
        ("relics", RELICS_OFF, RELICS_SIZE),
        ("potion_flags", POTION_FLAGS_OFF, POTION_FLAGS_SIZE),
        ("run", RUN_OFF, RUN_SIZE),
    ]


class RichObservationEncoder:
    """Encodes CombatState / RunManager state into the rich flat vector.

    Stateless apart from precomputed lookup maps; safe to share across envs
    in the same process (but each env typically owns one).
    """

    VERSION = RICH_OBS_VERSION
    SIZE = RICH_OBS_SIZE

    def __init__(self) -> None:
        # Local aliases avoid module-global lookups in the hot path.
        self._card_idx = CARD_ID_TO_IDX
        self._power_idx = POWER_ID_TO_IDX
        self._relic_idx = RELIC_NAME_TO_IDX
        self._potion_idx = POTION_ID_TO_IDX
        self._intent_idx = INTENT_TO_IDX
        self._boss_idx = BOSS_NAME_TO_IDX
        self._map_point_idx = MAP_POINT_TO_IDX

    # ------------------------------------------------------------------
    # Combat segments
    # ------------------------------------------------------------------

    def encode_combat(
        self,
        combat: CombatState,
        obs: np.ndarray | None = None,
        *,
        boss_id: int = 0,
    ) -> np.ndarray:
        """Encode combat-only observation (run segment stays zeroed).

        Used directly by the combat env; the run env calls this and then
        fills the run segment on top.
        """
        if obs is None:
            obs = np.zeros(RICH_OBS_SIZE, dtype=np.float32)
        obs[IDS_BOSS_OFF] = float(boss_id)

        player = combat.primary_player

        # --- hand: ids + scalar features ---
        hand = combat.hand
        for i in range(min(len(hand), NUM_HAND_SLOTS)):
            card = hand[i]
            obs[IDS_HAND_OFF + i] = self._card_idx.get(card.card_id, -1) + 1
            base = HAND_SCALARS_OFF + i * HAND_FEATURES
            try:
                cost_eff = combat.modified_card_cost(player, card)
            except Exception:
                cost_eff = max(0, card.cost)
            obs[base + 0] = max(0, cost_eff) / 5.0
            obs[base + 1] = max(0, card.star_cost) / 5.0
            obs[base + 2] = 1.0 if card.upgraded else 0.0
            obs[base + 3] = 1.0 if combat.can_play_card(card) else 0.0
            obs[base + 4] = (card.base_damage or 0) / 50.0
            obs[base + 5] = (card.base_block or 0) / 50.0
            obs[base + 6] = 1.0 if card.is_attack else 0.0
            obs[base + 7] = 1.0 if card.is_skill else 0.0
            obs[base + 8] = 1.0 if card.is_power else 0.0
            obs[base + 9] = 1.0 if card.is_ethereal else 0.0
            obs[base + 10] = 1.0 if card.exhausts else 0.0
            obs[base + 11] = 1.0 if (card.is_curse or card.is_status) else 0.0

        # --- pile bags + sizes + soul counts ---
        soul_counts = [0.0, 0.0, 0.0, 0.0]  # hand, draw, discard, exhaust
        for card in hand:
            if card.card_id == CardId.SOUL:
                soul_counts[0] += 1.0
        for pile_i, attr in enumerate(_PILE_ATTRS):
            pile = getattr(combat, attr)
            bag_base = PILE_BAGS_OFF + pile_i * PILE_BAG_SIZE
            for card in pile:
                ci = self._card_idx.get(card.card_id)
                if ci is not None:
                    obs[bag_base + ci] += 1.0 / BAG_COUNT_SCALE
                if card.card_id == CardId.SOUL:
                    soul_counts[pile_i + 1] += 1.0
            obs[PILE_SIZES_OFF + pile_i] = len(pile) / 20.0

        # --- player core ---
        pc = PLAYER_CORE_OFF
        obs[pc + 0] = player.current_hp / player.max_hp if player.max_hp > 0 else 0.0
        obs[pc + 1] = player.max_hp / 100.0
        obs[pc + 2] = player.block / 50.0
        obs[pc + 3] = combat.energy / 10.0
        obs[pc + 4] = combat.max_energy / 10.0
        obs[pc + 5] = combat.stars / 10.0
        obs[pc + 6] = combat.round_number / 50.0
        obs[pc + 7] = 1.0
        pending = combat.pending_choice
        obs[pc + 8] = 1.0 if pending is not None else 0.0
        obs[pc + 9] = (pending.num_options / 10.0) if pending is not None else 0.0

        # --- player powers (full PowerId space) ---
        for pid, pinst in player.powers.items():
            pi = self._power_idx.get(pid)
            if pi is not None:
                obs[PLAYER_POWERS_OFF + pi] = pinst.amount / POWER_AMOUNT_SCALE

        # --- Necrobinder: Osty + Souls + ally slots ---
        nb = NECRO_OFF
        osty = combat.get_osty(player)
        if osty is not None:
            obs[nb + 0] = 1.0 if osty.is_alive else 0.0
            obs[nb + 1] = osty.current_hp / osty.max_hp if osty.max_hp > 0 else 0.0
            obs[nb + 2] = osty.max_hp / 50.0
            obs[nb + 3] = osty.block / 50.0
        for i in range(4):
            obs[nb + NECRO_OSTY_SIZE + i] = soul_counts[i] / 10.0
        ally_base = nb + NECRO_OSTY_SIZE + NECRO_SOULS_SIZE
        for i, ally in enumerate(combat.allies[:NUM_ALLY_SLOTS]):
            b = ally_base + i * NECRO_ALLY_FEATURES
            obs[b + 0] = 1.0 if ally.is_alive else 0.0
            obs[b + 1] = ally.current_hp / ally.max_hp if ally.max_hp > 0 else 0.0
            obs[b + 2] = ally.block / 50.0

        # --- enemies ---
        for i in range(min(len(combat.enemies), MAX_ENEMIES)):
            enemy = combat.enemies[i]
            eb = ENEMIES_OFF + i * ENEMY_BLOCK_SIZE
            obs[eb + 0] = 1.0 if enemy.is_alive else 0.0
            obs[eb + 1] = enemy.current_hp / enemy.max_hp if enemy.max_hp > 0 else 0.0
            obs[eb + 2] = enemy.max_hp / 250.0
            obs[eb + 3] = enemy.block / 50.0
            ai = combat.enemy_ais.get(enemy.combat_id)
            if ai is not None and enemy.is_alive:
                move = ai.current_move
                if move is not None and move.intents:
                    intent = move.intents[0]
                    obs[eb + 4] = intent.damage / 30.0
                    obs[eb + 5] = intent.hits / 5.0
                    ii = self._intent_idx.get(intent.intent_type)
                    if ii is not None:
                        obs[eb + ENEMY_CORE_FEATURES + ii] = 1.0
            pw_base = eb + ENEMY_CORE_FEATURES + NUM_INTENT_TYPES
            for pid, pinst in enemy.powers.items():
                pi = self._power_idx.get(pid)
                if pi is not None:
                    obs[pw_base + pi] = pinst.amount / POWER_AMOUNT_SCALE

        # --- relics (combat-side RelicInstances) ---
        n_relics = 0
        for relic in combat.relics:
            rid = getattr(relic, "relic_id", None)
            name = getattr(rid, "name", None)
            ri = self._relic_idx.get(name) if name else None
            if ri is not None:
                obs[RELICS_OFF + ri] = 1.0
            n_relics += 1
        obs[RELICS_OFF + NUM_RELIC_IDS] = n_relics / 30.0

        # --- potions: ids + usable flags ---
        potions = combat.potions
        for i in range(min(len(potions), NUM_POTION_SLOTS)):
            potion = potions[i]
            if potion is None:
                continue
            obs[IDS_POTION_OFF + i] = self._potion_idx.get(potion.potion_id, -1) + 1
            try:
                usable = combat.can_use_potion(i, owner=player)
            except Exception:
                usable = True
            obs[POTION_FLAGS_OFF + i] = 1.0 if usable else 0.0

        return obs

    # ------------------------------------------------------------------
    # Run-level segments
    # ------------------------------------------------------------------

    def boss_id_for_manager(self, mgr: RunManager) -> int:
        """Resolve the current act's boss identity to a vocab index."""
        setup = getattr(mgr, "act_boss_setup", None)
        name = getattr(setup, "__name__", None)
        if name and name in self._boss_idx:
            return self._boss_idx[name]
        boss_ids = mgr.run_state.current_act.boss_ids
        if boss_ids:
            return self._boss_idx.get(boss_ids[0], 0)
        return 0

    def encode_run(self, mgr: RunManager) -> np.ndarray:
        """Encode the full run observation (combat segments + run segment)."""
        obs = np.zeros(RICH_OBS_SIZE, dtype=np.float32)
        rs = mgr.run_state
        player = rs.player
        boss_id = self.boss_id_for_manager(mgr)

        combat = mgr.get_combat_state()
        if combat is not None:
            self.encode_combat(combat, obs, boss_id=boss_id)
        else:
            obs[IDS_BOSS_OFF] = float(boss_id)
            # Out of combat: expose held potions in the ID block (usable
            # flags stay 0 -- potions cannot be drunk outside combat here).
            for i in range(min(len(player.potions), NUM_POTION_SLOTS)):
                potion = player.potions[i]
                if potion is not None:
                    obs[IDS_POTION_OFF + i] = self._potion_idx.get(potion.potion_id, -1) + 1

        r = RUN_OFF

        # act index (norm + one-hot)
        act = rs.current_act_index
        obs[r + RUN_ACT_OFF] = act / 3.0
        if 0 <= act < NUM_ACTS_ONEHOT:
            obs[r + RUN_ACT_OFF + 1 + act] = 1.0

        # floors
        obs[r + RUN_FLOOR_OFF + 0] = rs.act_floor / 20.0
        obs[r + RUN_FLOOR_OFF + 1] = rs.total_floor / 60.0

        # hp / gold
        obs[r + RUN_HP_GOLD_OFF + 0] = player.current_hp / max(player.max_hp, 1)
        obs[r + RUN_HP_GOLD_OFF + 1] = player.max_hp / 100.0
        obs[r + RUN_HP_GOLD_OFF + 2] = player.gold / 1000.0

        # Act 4 Heart keys (they are relics; also given an explicit segment)
        relic_names = set(player.relics)
        keys = 0
        for ki, key_name in enumerate(("EMERALD_KEY", "RUBY_KEY", "SAPPHIRE_KEY")):
            if key_name in relic_names:
                obs[r + RUN_KEYS_OFF + ki] = 1.0
                keys += 1
        obs[r + RUN_KEYS_OFF + 3] = keys / 3.0

        # deck aggregates
        deck = player.deck
        n = len(deck)
        d = r + RUN_DECK_OFF
        obs[d + 0] = n / 40.0
        if n:
            n_att = n_skill = n_pow = n_curse = n_upg = 0
            cost_sum = 0
            for card in deck:
                cost_sum += max(0, card.cost)
                if card.upgraded:
                    n_upg += 1
                if card.is_attack:
                    n_att += 1
                elif card.is_skill:
                    n_skill += 1
                elif card.is_power:
                    n_pow += 1
                if card.card_type == CardType.CURSE:
                    n_curse += 1
            obs[d + 1] = (cost_sum / n) / 3.0
            obs[d + 2] = n_upg / n
            obs[d + 3] = n_curse / 5.0
            obs[d + 4] = n_att / n
            obs[d + 5] = n_skill / n
            obs[d + 6] = n_pow / n

        # act-slot candidate selection (reads the registry dynamically)
        for slot in range(NUM_ACT_SLOTS):
            base = r + RUN_ACT_CAND_OFF + slot * (MAX_ACT_CANDIDATES + 1)
            chosen = rs.acts[slot] if slot < len(rs.acts) else None
            if chosen is None:
                continue
            candidates = act_candidates_for_slot(slot)
            sel = 0
            for ci, cand in enumerate(candidates[:MAX_ACT_CANDIDATES]):
                if cand.is_legacy == chosen.is_legacy and list(cand.boss_ids) == list(chosen.boss_ids):
                    sel = ci
                    break
            obs[base + sel] = 1.0
            obs[base + MAX_ACT_CANDIDATES] = 1.0 if chosen.is_legacy else 0.0

        # map lookahead: room-type counts reachable in the next 3 rows
        self._encode_map_lookahead(rs, obs, r + RUN_LOOKAHEAD_OFF)

        # phase one-hot + reward-subscreen flags
        phase_idx = RUN_PHASE_INDEX.get(mgr.phase, 0)
        obs[r + RUN_PHASE_OFF + phase_idx] = 1.0
        flags = r + RUN_PHASE_OFF + NUM_RUN_PHASES
        obs[flags + 0] = 1.0 if getattr(mgr, "_offered_potion", None) is not None else 0.0
        obs[flags + 1] = 1.0 if getattr(mgr, "_offered_relic", None) is not None else 0.0
        obs[flags + 2] = 1.0 if rs.pending_choice is not None else 0.0

        # misc
        obs[r + RUN_MISC_OFF + 0] = rs.ascension_level / 20.0
        room = getattr(mgr, "_current_room_type", None)
        obs[r + RUN_MISC_OFF + 1] = 1.0 if room == RoomType.ELITE else 0.0
        obs[r + RUN_MISC_OFF + 2] = 1.0 if room == RoomType.BOSS else 0.0

        return obs

    def _encode_map_lookahead(self, rs: Any, obs: np.ndarray, base: int) -> None:
        act_map = rs.map
        if act_map is None:
            return
        # Current node: last visited coord, else the start point.
        point = None
        if rs.visited_map_coords:
            point = act_map.get_point(rs.visited_map_coords[-1])
        if point is None:
            point = act_map.start_point
        if point is None:
            return
        frontier = [point]
        for row in range(MAP_LOOKAHEAD_ROWS):
            nxt: list[Any] = []
            seen: set[tuple[int, int]] = set()
            for p in frontier:
                for child in p.children:
                    key = (child.col, child.row)
                    if key in seen:
                        continue
                    seen.add(key)
                    nxt.append(child)
                    mi = self._map_point_idx.get(child.point_type)
                    if mi is not None:
                        obs[base + row * NUM_MAP_POINT_TYPES + mi] += 0.25
            frontier = nxt
            if not frontier:
                break
