"""Combat-only Gymnasium environment with the rich observation (v1).

Same ``Discrete(115)`` combat action space and masking as
:class:`~sts2_env.gym_env.combat_env.STS2CombatEnv`, but:

* observation is the rich vector from :mod:`sts2_env.gym_env.rich_observation`
  (run-level segment stays zeroed, by design, so weights transfer to the
  full-run env);
* the encounter pool is parameterized over acts (including legacy-act and
  Act 4 Heart pools);
* the deck can be sampled per-reset ("starter" or "progressive" mid-run
  decks) for curriculum stage B;
* reward follows :class:`~sts2_env.gym_env.reward_config.RewardConfig`
  (terminal win/death + annealable combat HP-retention shaping).
"""

from __future__ import annotations

import logging
from typing import Callable, Sequence

import gymnasium
import numpy as np
from gymnasium import spaces

from sts2_env.cards.base import CardInstance, reset_instance_counter
from sts2_env.cards.factory import create_card
from sts2_env.characters import get_character
from sts2_env.core.combat import CombatState
from sts2_env.core.constants import ACTION_END_TURN, ACTION_SPACE_SIZE
from sts2_env.core.rng import INT_MAX_EXCLUSIVE, Rng
from sts2_env.gym_env.action_space import (
    action_to_card_and_target,
    action_to_potion_and_target,
    get_action_mask,
    is_potion_action,
)
from sts2_env.gym_env.reward_config import RewardConfig
from sts2_env.gym_env.rich_observation import (
    RICH_OBS_HIGH,
    RICH_OBS_LOW,
    RICH_OBS_SIZE,
    RichObservationEncoder,
)
from sts2_env.potions.base import PotionInstance, all_potion_models, create_potion

logger = logging.getLogger(__name__)

EncounterSetup = Callable[..., None]

# Known encounter pool names -> module paths. "thebeyond" is import-guarded:
# it is under concurrent development and joins the pool automatically once
# the module exists.
_POOL_MODULES: dict[str, str] = {
    "act1": "sts2_env.encounters.act1",
    "act2": "sts2_env.encounters.act2",
    "act3": "sts2_env.encounters.act3",
    "act4heart": "sts2_env.encounters.act4_heart",
    "exordium": "sts2_env.encounters.exordium",
    "thecity": "sts2_env.encounters.thecity",
    "thebeyond": "sts2_env.encounters.thebeyond",
}

DEFAULT_MAX_TURNS = 200


def resolve_encounter_pool(pool_names: Sequence[str]) -> list[EncounterSetup]:
    """Build a flat encounter list from named act pools (import-guarded)."""
    import importlib

    setups: list[EncounterSetup] = []
    for name in pool_names:
        mod_path = _POOL_MODULES.get(name)
        if mod_path is None:
            raise ValueError(f"Unknown encounter pool '{name}' (valid: {sorted(_POOL_MODULES)})")
        try:
            mod = importlib.import_module(mod_path)
        except ImportError:
            logger.warning("Encounter pool '%s' not importable yet; skipping", name)
            continue
        for attr in ("WEAK_ENCOUNTERS", "NORMAL_ENCOUNTERS", "ELITE_ENCOUNTERS", "BOSS_ENCOUNTERS"):
            setups.extend(getattr(mod, attr, []))
    if not setups:
        raise ValueError(f"No encounters resolved from pools {list(pool_names)}")
    return setups


# ---------------------------------------------------------------------------
# Deck samplers
# ---------------------------------------------------------------------------

# Combat-safe relics for the progressive sampler (computed lazily: shared-pool
# common/uncommon relics without pickup effects or pets).
_SAFE_RELIC_NAMES: list[str] | None = None


def _safe_relic_names() -> list[str]:
    global _SAFE_RELIC_NAMES
    if _SAFE_RELIC_NAMES is None:
        from sts2_env.relics.base import RelicPool, RelicRarity
        from sts2_env.relics.registry import RELIC_REGISTRY, load_all_relics

        load_all_relics()
        _SAFE_RELIC_NAMES = sorted(
            cls.relic_id.name
            for cls in RELIC_REGISTRY.values()
            if cls.rarity in (RelicRarity.COMMON, RelicRarity.UNCOMMON)
            and cls.pool == RelicPool.SHARED
            and not cls.has_upon_pickup_effect
            and not cls.spawns_pets
        )
    return _SAFE_RELIC_NAMES


class StarterDeckSampler:
    """Always the character's starter deck, starter relic, no potions."""

    def __init__(self, character_id: str):
        self.character = get_character(character_id)

    def __call__(self, np_random: np.random.Generator):
        from sts2_env.run.run_manager import _get_starter_deck

        deck = _get_starter_deck(self.character.character_id)
        relics = [self.character.starting_relic]
        potions: list[PotionInstance | None] = []
        hp_fraction = 1.0
        return deck, relics, potions, hp_fraction


class ProgressiveDeckSampler:
    """Sample plausible mid-run decks for curriculum stage B.

    Starter deck + 0..``max_added_cards`` cards from the character pool
    (some upgraded), a chance to upgrade starters, 0..2 extra combat-safe
    relics, 0..2 potions, and a starting HP fraction in [0.5, 1.0].
    """

    def __init__(
        self,
        character_id: str,
        max_added_cards: int = 15,
        upgrade_chance: float = 0.35,
        max_extra_relics: int = 2,
        max_potions: int = 2,
    ):
        self.character = get_character(character_id)
        self.card_pool = list(self.character.card_pool)
        self.max_added_cards = max_added_cards
        self.upgrade_chance = upgrade_chance
        self.max_extra_relics = max_extra_relics
        self.max_potions = max_potions
        self._potion_ids = sorted(
            m.potion_id
            for m in all_potion_models()
            if m.is_in_normal_pool and m.is_available_for_character(character_id)
        )

    def __call__(self, np_random: np.random.Generator):
        from sts2_env.run.run_manager import _get_starter_deck

        deck: list[CardInstance] = []
        for card in _get_starter_deck(self.character.character_id):
            if np_random.random() < self.upgrade_chance * 0.5:
                card = create_card(card.card_id, upgraded=True)
            deck.append(card)

        n_added = int(np_random.integers(0, self.max_added_cards + 1))
        for _ in range(n_added):
            cid = self.card_pool[int(np_random.integers(0, len(self.card_pool)))]
            upgraded = bool(np_random.random() < self.upgrade_chance)
            try:
                deck.append(create_card(cid, upgraded=upgraded))
            except Exception:  # unimplemented pool card -- skip
                continue

        relics: list = [self.character.starting_relic]
        safe = _safe_relic_names()
        n_relics = int(np_random.integers(0, self.max_extra_relics + 1))
        picks = np_random.choice(len(safe), size=min(n_relics, len(safe)), replace=False)
        relics.extend(safe[int(i)] for i in np.atleast_1d(picks))

        potions: list[PotionInstance | None] = []
        n_potions = int(np_random.integers(0, self.max_potions + 1))
        for slot in range(n_potions):
            pid = self._potion_ids[int(np_random.integers(0, len(self._potion_ids)))]
            try:
                potions.append(create_potion(pid, slot=slot))
            except Exception:
                potions.append(None)

        hp_fraction = 0.5 + 0.5 * float(np_random.random())
        return deck, relics, potions, hp_fraction


# ---------------------------------------------------------------------------
# Env
# ---------------------------------------------------------------------------

class RichSTS2CombatEnv(gymnasium.Env):
    """Single-combat env with the rich observation.

    Parameters
    ----------
    character_id : character to play (default ``"Necrobinder"``).
    ascension_level : ascension for monster scaling (default 10).
    encounter_pools : names of act pools sampled uniformly each reset
        (default ``("act1",)``). See ``_POOL_MODULES`` for valid names.
    encounter_pool : explicit list of encounter setups; overrides
        ``encounter_pools`` when given.
    deck_sampler : ``"starter"``, ``"progressive"``, or a callable
        ``(np_random) -> (deck, relics, potions, hp_fraction)``.
    reward_config : reward terms; ``shaping_scale`` is annealable via
        :meth:`set_shaping_scale`.
    max_turns : combat-turn cap; exceeding it truncates as a loss.
    max_episode_steps : env-step cap; exceeding it truncates as a loss
        (an agent can take many steps per turn, so a pure turn cap does
        not bound episode length).
    """

    metadata = {"render_modes": ["ansi"]}

    def __init__(
        self,
        character_id: str = "Necrobinder",
        ascension_level: int = 10,
        encounter_pools: Sequence[str] = ("act1",),
        encounter_pool: list[EncounterSetup] | None = None,
        deck_sampler: str | Callable = "starter",
        reward_config: RewardConfig | None = None,
        max_turns: int = DEFAULT_MAX_TURNS,
        max_episode_steps: int = 1000,
        render_mode: str | None = None,
    ):
        super().__init__()
        self.observation_space = spaces.Box(
            low=RICH_OBS_LOW, high=RICH_OBS_HIGH, shape=(RICH_OBS_SIZE,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(ACTION_SPACE_SIZE)

        self.character = get_character(character_id)
        self.character_id = self.character.character_id
        self.ascension_level = ascension_level
        self.encounter_pool = (
            list(encounter_pool) if encounter_pool is not None
            else resolve_encounter_pool(encounter_pools)
        )
        if deck_sampler == "starter":
            self.deck_sampler = StarterDeckSampler(self.character_id)
        elif deck_sampler == "progressive":
            self.deck_sampler = ProgressiveDeckSampler(self.character_id)
        elif callable(deck_sampler):
            self.deck_sampler = deck_sampler
        else:
            raise ValueError(f"Unknown deck_sampler: {deck_sampler!r}")

        self.reward_config = reward_config or RewardConfig()
        self.max_turns = max_turns
        self.max_episode_steps = max_episode_steps
        self.render_mode = render_mode

        self._encoder = RichObservationEncoder()
        self.combat: CombatState | None = None
        self._hp_start: int = 1
        self._step_count: int = 0

    # ------------------------------------------------------------------

    def set_shaping_scale(self, scale: float) -> None:
        self.reward_config.shaping_scale = scale
        self.reward_config.clamp()

    # ------------------------------------------------------------------

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)
        reset_instance_counter()

        rng_seed = int(self.np_random.integers(0, INT_MAX_EXCLUSIVE))
        rng = Rng(rng_seed)

        deck, relics, potions, hp_fraction = self.deck_sampler(self.np_random)
        max_hp = self.character.starting_hp
        hp = max(1, int(round(max_hp * hp_fraction)))

        self.combat = CombatState(
            player_hp=hp,
            player_max_hp=max_hp,
            deck=deck,
            rng_seed=rng_seed,
            character_id=self.character_id,
            relics=relics,
            potions=potions or None,
            ascension_level=self.ascension_level,
        )

        encounter_idx = int(self.np_random.integers(0, len(self.encounter_pool)))
        self.encounter_pool[encounter_idx](self.combat, rng)
        self.combat.start_combat()

        self._hp_start = self.combat.primary_player.current_hp
        self._step_count = 0

        obs = self._encoder.encode_combat(self.combat)
        info = {"action_mask": get_action_mask(self.combat)}
        return obs, info

    def step(self, action: int):
        assert self.combat is not None, "Must call reset() first"
        combat = self.combat
        self._step_count += 1

        if combat.pending_choice is not None:
            if action == ACTION_END_TURN:
                combat.resolve_pending_choice(None)
            else:
                combat.resolve_pending_choice(action - 1)
        else:
            if action == ACTION_END_TURN:
                combat.end_player_turn()
            elif is_potion_action(action):
                slot_idx, target_idx = action_to_potion_and_target(action)
                if not (slot_idx is not None and combat.use_potion(slot_idx, target_index=target_idx)):
                    logger.debug("Ignored invalid potion action %d", action)
            else:
                hand_idx, target_idx = action_to_card_and_target(action)
                if not (hand_idx is not None and combat.play_card(hand_idx, target_idx)):
                    logger.debug("Ignored invalid card action %d", action)

        terminated = combat.is_over
        truncated = not terminated and (
            combat.turn_count > self.max_turns
            or self._step_count >= self.max_episode_steps
        )

        cfg = self.reward_config
        reward = 0.0
        won = False
        if terminated:
            won = combat.player_won
            reward = cfg.terminal_reward(won)
            if won:
                reward += cfg.combat_win_reward(
                    self._hp_start, combat.primary_player.current_hp
                )
        elif truncated:
            reward = cfg.death

        obs = self._encoder.encode_combat(combat)
        info = {"action_mask": get_action_mask(combat)}
        if terminated or truncated:
            info["won"] = won
        return obs, float(reward), terminated, truncated, info

    def action_masks(self) -> np.ndarray:
        if self.combat is None:
            mask = np.zeros(ACTION_SPACE_SIZE, dtype=np.int8)
            mask[0] = 1
            return mask
        return get_action_mask(self.combat)

    def render(self):
        if self.render_mode == "ansi" and self.combat is not None:
            return str(self.combat)
        return None
