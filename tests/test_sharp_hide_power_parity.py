"""SharpHidePower (ActsFromThePast) parity tests.

Ground truth:
  decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/SharpHidePower.cs
  decompiled_mods/ActsFromThePast/ActsFromThePast/Guardian.cs  (the one consumer
  of AttackInProgress / AttackSource)

Sharp Hide is the power the simulator was nearly given as an alias of Thorns.
It is not Thorns: it fires from AfterCardPlayed once per Attack CARD, not from
BeforeDamageReceived once per hit that lands. Every test below is written to
fail if the implementation ever collapses back onto Thorns, so each
distinguishing case is asserted against a Thorns control in the same combat
shape.
"""

import sts2_env.powers  # noqa: F401

from sts2_env.cards.ironclad import make_twin_strike
from sts2_env.cards.ironclad_basic import make_defend_ironclad, make_strike_ironclad
from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.creature import get_power_class
from sts2_env.core.enums import PowerId, PowerStackType, PowerType, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.monsters.act1_weak import create_shrinker_beetle
from sts2_env.powers.damage_reactions import SharpHidePower


REFERENCE_PLAYER_HP = 80
REFERENCE_SEED = 84
#: Guardian.cs:85 -- SharpHideThorns is 3, or 4 from A9. Any value works; 3 is
#: the one an agent will actually meet.
SHARP_HIDE_AMOUNT = 3
STRIKE_DAMAGE = 6
TWIN_STRIKE_DAMAGE_PER_HIT = 5
TWIN_STRIKE_HITS = 2
REFERENCE_ENEMY_HP = 60


def _make_combat(enemy_count: int = 1) -> CombatState:
    combat = CombatState(
        player_hp=REFERENCE_PLAYER_HP,
        player_max_hp=REFERENCE_PLAYER_HP,
        deck=create_ironclad_starter_deck(),
        rng_seed=REFERENCE_SEED,
        character_id="Ironclad",
    )
    for _ in range(enemy_count):
        creature, ai = create_shrinker_beetle(Rng(REFERENCE_SEED))
        combat.add_enemy(creature, ai)
    combat.start_combat()
    for enemy in combat.enemies:
        enemy.max_hp = REFERENCE_ENEMY_HP
        enemy.current_hp = REFERENCE_ENEMY_HP
    return combat


def _arm(combat: CombatState, *cards, energy: int = 3) -> None:
    combat.hand = list(cards)
    combat.energy = energy


class TestSharpHideRetaliation:
    def test_playing_an_attack_retaliates_for_amount(self):
        """SharpHidePower.cs:41-49 -- AfterCardPlayed on an Attack damages the card owner."""
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(combat, make_strike_ironclad())
        player_hp = combat.player.current_hp

        assert combat.play_card(0, 0)

        assert enemy.current_hp == REFERENCE_ENEMY_HP - STRIKE_DAMAGE
        assert combat.player.current_hp == player_hp - SHARP_HIDE_AMOUNT

    def test_playing_a_skill_does_not_retaliate(self):
        """SharpHidePower.cs:41 -- the retaliation is gated on CardType.Attack (== 1)."""
        combat = _make_combat()
        combat.enemies[0].apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(combat, make_defend_ironclad())
        player_hp = combat.player.current_hp

        assert combat.play_card(0)

        assert combat.player.current_hp == player_hp

    def test_the_retaliation_amount_is_the_power_amount_and_stacks(self):
        """StackType is (PowerStackType)1 == Counter: re-application sums amounts."""
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        enemy.apply_power(PowerId.SHARP_HIDE, 2)
        assert enemy.get_power_amount(PowerId.SHARP_HIDE) == SHARP_HIDE_AMOUNT + 2
        _arm(combat, make_strike_ironclad())
        player_hp = combat.player.current_hp

        assert combat.play_card(0, 0)

        assert combat.player.current_hp == player_hp - (SHARP_HIDE_AMOUNT + 2)

    def test_the_power_does_not_decay_and_fires_again_next_turn(self):
        """There is no tick hook in the .cs -- Sharp Hide persists until removed."""
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(combat, make_strike_ironclad())
        assert combat.play_card(0, 0)
        assert enemy.get_power_amount(PowerId.SHARP_HIDE) == SHARP_HIDE_AMOUNT

        combat.end_player_turn()
        assert enemy.get_power_amount(PowerId.SHARP_HIDE) == SHARP_HIDE_AMOUNT

        _arm(combat, make_strike_ironclad())
        player_hp = combat.player.current_hp
        assert combat.play_card(0, 0)
        assert combat.player.current_hp == player_hp - SHARP_HIDE_AMOUNT


class TestSharpHideIsNotThorns:
    """Each case here is a line where Thorns and Sharp Hide disagree."""

    def test_a_multi_hit_attack_retaliates_once_where_thorns_retaliates_per_hit(self):
        """AfterCardPlayed fires once per CardPlay; ThornsPower.cs fires per
        BeforeDamageReceived, i.e. once per hit."""
        sharp = _make_combat()
        sharp.enemies[0].apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(sharp, make_twin_strike())
        sharp_hp = sharp.player.current_hp
        assert sharp.play_card(0, 0)

        # Both hits really landed, so the contrast below is about trigger
        # count and not about the card fizzling.
        assert sharp.enemies[0].current_hp == (
            REFERENCE_ENEMY_HP - TWIN_STRIKE_DAMAGE_PER_HIT * TWIN_STRIKE_HITS
        )
        assert sharp.player.current_hp == sharp_hp - SHARP_HIDE_AMOUNT

        thorns = _make_combat()
        thorns.enemies[0].apply_power(PowerId.THORNS, SHARP_HIDE_AMOUNT)
        _arm(thorns, make_twin_strike())
        thorns_hp = thorns.player.current_hp
        assert thorns.play_card(0, 0)

        assert thorns.player.current_hp == thorns_hp - SHARP_HIDE_AMOUNT * TWIN_STRIKE_HITS

    def test_an_attack_aimed_at_another_enemy_still_retaliates(self):
        """The .cs never inspects the attack's target -- only cardPlay.Card.Type.
        Thorns needs `target == base.Owner` and so stays silent."""
        sharp = _make_combat(enemy_count=2)
        front, back = sharp.enemies
        back.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(sharp, make_strike_ironclad())
        sharp_hp = sharp.player.current_hp
        assert sharp.play_card(0, 0)

        assert front.current_hp == REFERENCE_ENEMY_HP - STRIKE_DAMAGE
        assert back.current_hp == REFERENCE_ENEMY_HP
        assert sharp.player.current_hp == sharp_hp - SHARP_HIDE_AMOUNT

        thorns = _make_combat(enemy_count=2)
        thorns.enemies[1].apply_power(PowerId.THORNS, SHARP_HIDE_AMOUNT)
        _arm(thorns, make_strike_ironclad())
        thorns_hp = thorns.player.current_hp
        assert thorns.play_card(0, 0)

        assert thorns.player.current_hp == thorns_hp

    def test_a_fully_blocked_attack_still_retaliates(self):
        """The .cs does not look at the DamageResult, so 0 damage through the
        owner's Block changes nothing."""
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.block = STRIKE_DAMAGE * 2
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(combat, make_strike_ironclad())
        player_hp = combat.player.current_hp

        assert combat.play_card(0, 0)

        assert enemy.current_hp == REFERENCE_ENEMY_HP
        assert combat.player.current_hp == player_hp - SHARP_HIDE_AMOUNT

    def test_an_attack_that_kills_the_owner_is_not_punished(self):
        """The timing detail. AfterCardPlayed runs after the card resolves, by
        which point a dead owner's powers are gone -- Guardian.cs:367-395 only
        exists because of this. Thorns, firing from BeforeDamageReceived, does
        punish the killing blow."""
        sharp = _make_combat(enemy_count=2)
        sharp.enemies[0].current_hp = 1
        sharp.enemies[0].apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(sharp, make_strike_ironclad())
        sharp_hp = sharp.player.current_hp
        assert sharp.play_card(0, 0)

        assert sharp.enemies[0].is_dead
        assert sharp.player.current_hp == sharp_hp

        thorns = _make_combat(enemy_count=2)
        thorns.enemies[0].current_hp = 1
        thorns.enemies[0].apply_power(PowerId.THORNS, SHARP_HIDE_AMOUNT)
        _arm(thorns, make_strike_ironclad())
        thorns_hp = thorns.player.current_hp
        assert thorns.play_card(0, 0)

        assert thorns.enemies[0].is_dead
        assert thorns.player.current_hp == thorns_hp - SHARP_HIDE_AMOUNT


class TestSharpHideDamageShape:
    def test_the_retaliation_is_unpowered_with_no_dealer(self):
        """CreatureCmd.cs:118 -- the 6-arg overload derives the dealer from
        `cardSource?.Owner.Creature`, and SharpHide passes cardSource = null.
        (ValueProp)4 is Unpowered alone."""
        combat = _make_combat()
        combat.enemies[0].apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        _arm(combat, make_strike_ironclad())

        assert combat.play_card(0, 0)

        hits_on_player = [
            event for event in combat._damage_events_this_turn if event[1] is combat.player
        ]
        assert hits_on_player == [(None, combat.player, ValueProp.UNPOWERED)]

    def test_the_retaliation_is_absorbed_by_block(self):
        """Unpowered carries no Unblockable bit, so Block still applies."""
        combat = _make_combat()
        combat.enemies[0].apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        combat.player.block = SHARP_HIDE_AMOUNT + 4
        _arm(combat, make_strike_ironclad())
        player_hp = combat.player.current_hp

        assert combat.play_card(0, 0)

        assert combat.player.current_hp == player_hp
        assert combat.player.block == 4


class TestSharpHideAttackInProgressFlag:
    """BeforeCardPlayed/AfterCardPlayed maintain the pair the Guardian reads
    from BeforeDeath (Guardian.cs:373-391)."""

    def _armed(self):
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        power = enemy.powers[PowerId.SHARP_HIDE]
        assert isinstance(power, SharpHidePower)
        return combat, enemy, power

    def test_the_flag_starts_closed(self):
        _, _, power = self._armed()
        assert power.attack_in_progress is False
        assert power.attack_source is None

    def test_before_card_played_opens_the_window_on_an_attack(self):
        """SharpHidePower.cs:28-33 -- AttackInProgress = true, AttackSource =
        cardPlay.Card.Owner.Creature."""
        combat, enemy, power = self._armed()
        strike = make_strike_ironclad()
        strike.owner = combat.player

        power.before_card_played(enemy, strike, combat)

        assert power.attack_in_progress is True
        assert power.attack_source is combat.player

    def test_before_card_played_leaves_the_window_shut_for_a_skill(self):
        combat, enemy, power = self._armed()
        defend = make_defend_ironclad()
        defend.owner = combat.player

        power.before_card_played(enemy, defend, combat)

        assert power.attack_in_progress is False
        assert power.attack_source is None

    def test_after_card_played_closes_the_window_and_retaliates(self):
        combat, enemy, power = self._armed()
        strike = make_strike_ironclad()
        strike.owner = combat.player
        power.before_card_played(enemy, strike, combat)
        player_hp = combat.player.current_hp

        power.after_card_played(enemy, strike, combat)

        assert power.attack_in_progress is False
        assert power.attack_source is None
        assert combat.player.current_hp == player_hp - SHARP_HIDE_AMOUNT

    def test_any_card_closes_the_window_even_a_skill(self):
        """SharpHidePower.cs:39-40 -- the two assignments sit ABOVE the
        card-type check, so a Skill closes an open window without retaliating."""
        combat, enemy, power = self._armed()
        strike = make_strike_ironclad()
        strike.owner = combat.player
        defend = make_defend_ironclad()
        defend.owner = combat.player
        power.before_card_played(enemy, strike, combat)
        assert power.attack_in_progress is True
        player_hp = combat.player.current_hp

        power.after_card_played(enemy, defend, combat)

        assert power.attack_in_progress is False
        assert power.attack_source is None
        assert combat.player.current_hp == player_hp

    def test_a_real_card_play_leaves_the_window_shut(self):
        combat, _, power = self._armed()
        _arm(combat, make_strike_ironclad())

        assert combat.play_card(0, 0)

        assert power.attack_in_progress is False
        assert power.attack_source is None

    def test_no_retaliation_when_the_card_owner_is_dead(self):
        """SharpHidePower.cs:46 -- `if (player != null && player.IsAlive)`."""
        combat, enemy, power = self._armed()
        strike = make_strike_ironclad()
        strike.owner = combat.player
        combat.player.current_hp = 0

        power.after_card_played(enemy, strike, combat)

        assert combat.player.current_hp == 0


class TestSharpHideRegistration:
    def test_the_power_id_resolves_to_the_implementation(self):
        assert get_power_class(PowerId.SHARP_HIDE) is SharpHidePower

    def test_power_type_and_stack_type_match_the_cs(self):
        """(PowerType)1 == Buff, (PowerStackType)1 == Counter."""
        assert SharpHidePower.power_type is PowerType.BUFF
        assert SharpHidePower.stack_type is PowerStackType.COUNTER

    def test_applying_it_by_id_constructs_the_implementation(self):
        combat = _make_combat()
        enemy = combat.enemies[0]
        enemy.apply_power(PowerId.SHARP_HIDE, SHARP_HIDE_AMOUNT)
        assert isinstance(enemy.powers[PowerId.SHARP_HIDE], SharpHidePower)
        assert enemy.get_power_amount(PowerId.SHARP_HIDE) == SHARP_HIDE_AMOUNT
