"""Tests for the live card-value preview (``sts2_env.content.preview``).

The preview must reproduce the simulator's own combat math (Strength,
Vulnerable, Weak, Frail, cost modifiers, relic hooks) for the values a hand
card would produce right now — without mutating any combat state.
"""

from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
from sts2_env.content import card_preview
from sts2_env.core.combat import CombatState
from sts2_env.core.damage import calculate_block, calculate_damage
from sts2_env.core.enums import CardType, PowerId, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.monsters.act1_weak import create_shrinker_beetle


STRIKE_BASE_DAMAGE = 6
DEFEND_BASE_BLOCK = 5
STRENGTH_AMOUNT = 3
VULNERABLE_AMOUNT = 2
FRAIL_AMOUNT = 2
BORROWED_TIME_AMOUNT = 1
VAMBRACE_RELIC_NAME = "VAMBRACE"


def _make_combat(enemy_count: int = 1, relics: list[str] | None = None) -> CombatState:
    rng = Rng(42)
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=42,
        relics=relics,
    )
    for _ in range(enemy_count):
        creature, ai = create_shrinker_beetle(rng)
        combat.add_enemy(creature, ai)
    combat.start_combat()
    return combat


def _hand_card(combat: CombatState, *, damage: bool) -> object:
    for pile in (combat.hand, combat.draw_pile, combat.discard_pile):
        for card in pile:
            if damage and card.base_damage == STRIKE_BASE_DAMAGE and card.card_type == CardType.ATTACK:
                return card
            if not damage and card.base_block == DEFEND_BASE_BLOCK:
                return card
    raise AssertionError("starter deck card not found")


def test_unmodified_card_previews_base_values() -> None:
    combat = _make_combat()
    strike = _hand_card(combat, damage=True)
    defend = _hand_card(combat, damage=False)

    strike_preview = card_preview(strike, combat, combat.primary_player)
    defend_preview = card_preview(defend, combat, combat.primary_player)

    assert strike_preview["base_damage"] == STRIKE_BASE_DAMAGE
    assert [entry["value"] for entry in strike_preview["eff_damage_by_target"]] == [STRIKE_BASE_DAMAGE]
    assert strike_preview["eff_cost"] == strike.cost
    assert strike_preview["flags"] == {}
    assert defend_preview["eff_block"] == DEFEND_BASE_BLOCK
    assert defend_preview["flags"] == {}


def test_strength_increases_previewed_damage() -> None:
    combat = _make_combat()
    player = combat.primary_player
    enemy = combat.enemies[0]
    combat.apply_power_to(player, PowerId.STRENGTH, STRENGTH_AMOUNT)
    strike = _hand_card(combat, damage=True)

    preview = card_preview(strike, combat, player)

    expected = calculate_damage(
        STRIKE_BASE_DAMAGE, player, enemy, ValueProp.MOVE, [player, enemy]
    )
    assert expected == STRIKE_BASE_DAMAGE + STRENGTH_AMOUNT  # 9
    assert [entry["value"] for entry in preview["eff_damage_by_target"]] == [expected]
    assert preview["flags"].get("damage_up") is True
    assert "damage_down" not in preview["flags"]


def test_vulnerable_target_matches_pipeline_rounding() -> None:
    combat = _make_combat()
    player = combat.primary_player
    enemy = combat.enemies[0]
    combat.apply_power_to(player, PowerId.STRENGTH, STRENGTH_AMOUNT)
    combat.apply_power_to(enemy, PowerId.VULNERABLE, VULNERABLE_AMOUNT)
    strike = _hand_card(combat, damage=True)

    preview = card_preview(strike, combat, player)

    # Expected value comes THROUGH the sim's own pipeline, not a re-derived
    # formula — this pins the exact rounding: floor((6 + 3) * 1.5) = 13.
    expected = calculate_damage(
        STRIKE_BASE_DAMAGE, player, enemy, ValueProp.MOVE, [player, enemy]
    )
    assert expected == 13
    assert [entry["value"] for entry in preview["eff_damage_by_target"]] == [expected]


def test_per_target_damage_differs_when_one_enemy_is_vulnerable() -> None:
    combat = _make_combat(enemy_count=2)
    player = combat.primary_player
    combat.apply_power_to(combat.enemies[1], PowerId.VULNERABLE, VULNERABLE_AMOUNT)
    strike = _hand_card(combat, damage=True)

    preview = card_preview(strike, combat, player)

    values = {entry["enemy_index"]: entry["value"] for entry in preview["eff_damage_by_target"]}
    expected_vulnerable = calculate_damage(
        STRIKE_BASE_DAMAGE, player, combat.enemies[1], ValueProp.MOVE,
        [player, combat.enemies[1]],
    )
    assert values[0] == STRIKE_BASE_DAMAGE
    assert values[1] == expected_vulnerable
    assert values[1] > values[0]
    assert preview["flags"].get("damage_up") is True
    assert "damage_down" not in preview["flags"]


def test_frail_reduces_previewed_block() -> None:
    combat = _make_combat()
    player = combat.primary_player
    combat.apply_power_to(player, PowerId.FRAIL, FRAIL_AMOUNT)
    defend = _hand_card(combat, damage=False)

    preview = card_preview(defend, combat, player)

    expected = calculate_block(DEFEND_BASE_BLOCK, player, ValueProp.MOVE, [player])
    assert expected < DEFEND_BASE_BLOCK  # floor(5 * 0.75) = 3
    assert preview["eff_block"] == expected
    assert preview["flags"].get("block_down") is True


def test_borrowed_time_raises_previewed_cost() -> None:
    combat = _make_combat()
    player = combat.primary_player
    combat.apply_power_to(player, PowerId.BORROWED_TIME, BORROWED_TIME_AMOUNT)
    strike = _hand_card(combat, damage=True)

    preview = card_preview(strike, combat, player)

    assert preview["eff_cost"] == strike.cost + BORROWED_TIME_AMOUNT
    assert preview["flags"].get("cost_up") is True


def test_preview_includes_relic_block_hooks_without_state_leak() -> None:
    """Vambrace doubles the first block-granting card's block. The preview
    must show the doubled value AND leave the relic's one-shot tracking
    untouched so the real play still gets the bonus."""
    combat = _make_combat(relics=[VAMBRACE_RELIC_NAME])
    player = combat.primary_player
    defend = _hand_card(combat, damage=False)
    vambrace = next(relic for relic in combat.relics if relic.relic_id.name == VAMBRACE_RELIC_NAME)

    preview = card_preview(defend, combat, player)

    assert preview["eff_block"] == DEFEND_BASE_BLOCK * 2
    assert preview["flags"].get("block_up") is True
    # No state leaked: the relic never saw a "real" trigger and no attack
    # context was created.
    assert vambrace._triggering_card is None  # noqa: SLF001
    assert combat.pending_auto_attack is None
    assert combat.active_attack is None
    # A second preview (e.g. for another card) sees the same fresh state.
    assert card_preview(defend, combat, player)["eff_block"] == DEFEND_BASE_BLOCK * 2


def test_preview_is_repeatable_and_leaves_powers_untouched() -> None:
    combat = _make_combat()
    player = combat.primary_player
    combat.apply_power_to(player, PowerId.VIGOR, STRENGTH_AMOUNT)
    strike = _hand_card(combat, damage=True)

    first = card_preview(strike, combat, player)
    second = card_preview(strike, combat, player)

    assert first == second
    # Vigor is consumed on a real attack but must survive any number of previews.
    assert player.powers[PowerId.VIGOR].amount == STRENGTH_AMOUNT


def test_preview_never_crashes_on_damageless_cards_and_x_cost() -> None:
    from sts2_env.cards.factory import create_card
    from sts2_env.core.enums import CardId

    combat = _make_combat()
    player = combat.primary_player

    eradicate = create_card(CardId.ERADICATE)  # X-cost attack
    eradicate.owner = player
    preview = card_preview(eradicate, combat, player)
    assert preview["eff_cost"] is None  # X-cost has no fixed effective cost
    assert preview["eff_damage_by_target"]  # per-hit damage still previews

    bodyguard = create_card(CardId.BODYGUARD)  # no damage, no block
    bodyguard.owner = player
    preview = card_preview(bodyguard, combat, player)
    assert preview["eff_damage_by_target"] == []
    assert preview["eff_block"] is None
    assert preview["eff_cost"] == bodyguard.cost
