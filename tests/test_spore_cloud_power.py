"""SporeCloudPower ("Acts from the Past" mod, Exordium legacy-act FungiBeast).

Ground truth:
``decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/SporeCloudPower.cs``::

    public override async Task AfterDeath(PlayerChoiceContext choiceContext,
        Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != ((PowerModel)this).Owner) return;
        IEnumerable<Creature> players =
            ((PowerModel)this).CombatState.PlayerCreatures.Where((Creature c) => c.IsAlive);
        if (!players.Any()) return;
        ((PowerModel)this).Flash();
        AFTPModAudio.Play("fungi_beast", "spore_cloud_release");
        foreach (Creature player in players)
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(),
                player, (decimal)((PowerModel)this).Amount, (Creature)null, (CardModel)null, false);
    }

Four things distinguish it from "some on-death effect" and each gets its own
test: it is ``AfterDeath`` not ``BeforeDeath`` (so a prevented removal
suppresses it entirely), it only reacts to the OWNER's death, it targets
``IsPlayer`` creatures rather than the player SIDE, and dead players are
skipped.
"""

from __future__ import annotations

import sts2_env.powers  # noqa: F401 -- registers every power class

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.creature import Creature, get_power_class
from sts2_env.core.enums import PowerId, PowerStackType, PowerType, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.monsters.exordium import create_fungi_beast
from sts2_env.monsters.act1_weak import create_shrinker_beetle
from sts2_env.potions.base import create_potion
from sts2_env.powers.monster import SporeCloudPower
from sts2_env.run.run_state import PlayerState

import sts2_env.potions.all  # noqa: F401 -- registers FairyInABottle


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_combat(seed: int = 4242, *, player_hp: int = 80, extra_enemy: bool = False):
    """Combat with a Fungi Beast carrying Spore Cloud 2, as the mod sets it up.

    ``create_fungi_beast`` now self-applies Spore Cloud 2 on spawn, matching
    ``FungiBeast.AfterAddedToRoom`` (FungiBeast.cs:48). This helper used to
    apply it by hand because the monster wiring was missing; doing that now
    would stack it to 4 and every assertion below would be measuring double.

    That the beast arrives with it is itself worth asserting, so the wiring
    cannot quietly regress and leave these tests passing against a hand-applied
    power that the real monster no longer has.
    """
    combat = CombatState(
        player_hp=player_hp,
        player_max_hp=player_hp,
        deck=create_ironclad_starter_deck(),
        rng_seed=seed,
        character_id="Ironclad",
    )
    beast, ai = create_fungi_beast(Rng(seed))
    combat.add_enemy(beast, ai)
    other = None
    if extra_enemy:
        other, other_ai = create_shrinker_beetle(Rng(seed + 1))
        combat.add_enemy(other, other_ai)
    combat.start_combat()
    assert beast.get_power_amount(PowerId.SPORE_CLOUD) == 2, (
        "create_fungi_beast should self-apply Spore Cloud 2 on spawn; if that "
        "wiring is lost these tests must fail rather than silently fall back "
        "to a hand-applied power the real monster does not carry"
    )
    return combat, beast, other


def _vulnerable(creature: Creature) -> int:
    return creature.get_power_amount(PowerId.VULNERABLE)


# ---------------------------------------------------------------------------
# Metadata / registration
# ---------------------------------------------------------------------------

def test_power_is_registered_with_the_stack_semantics_the_cs_declares():
    # `Type => (PowerType)1` is Buff and `StackType => (PowerStackType)1` is
    # Counter in MegaCrit.Sts2.Core.Entities.Powers (both enums start at None).
    assert get_power_class(PowerId.SPORE_CLOUD) is SporeCloudPower
    assert SporeCloudPower.power_type is PowerType.BUFF
    assert SporeCloudPower.stack_type is PowerStackType.COUNTER


def test_it_is_an_after_death_effect_and_nothing_else():
    # The .cs overrides AfterDeath only -- no BeforeDeath, no turn hook. A
    # BeforeDeath implementation would fire before death prevention resolves,
    # which test_prevented_removal_... below shows is observably different.
    assert "after_death" in vars(SporeCloudPower)
    assert "before_death" not in vars(SporeCloudPower)


# ---------------------------------------------------------------------------
# The core trigger
# ---------------------------------------------------------------------------

def test_owner_death_applies_vulnerable_equal_to_amount():
    combat, beast, _ = _make_combat()
    assert _vulnerable(combat.player) == 0

    assert combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 2


def test_amount_is_read_off_the_power_and_stacks_onto_existing_vulnerable():
    combat, beast, _ = _make_combat()
    beast.powers[PowerId.SPORE_CLOUD].amount = 5
    combat.player.apply_power(PowerId.VULNERABLE, 1)

    combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 6


def test_applier_is_none_so_nothing_the_dying_owner_carries_scales_it():
    # PowerCmd.Apply is called with `(Creature)null` as the source.
    combat, beast, _ = _make_combat()
    beast.apply_power(PowerId.STRENGTH, 9, applier=beast)

    combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 2
    assert combat.player.powers[PowerId.VULNERABLE].applier is None


def test_it_fires_when_the_owner_is_killed_by_damage_not_only_by_kill_creature():
    combat, beast, other = _make_combat(extra_enemy=True)
    beast.current_hp = 3

    combat.deal_damage(
        dealer=combat.player,
        target=beast,
        amount=50,
        props=ValueProp.UNPOWERED,
    )

    assert beast.is_dead
    assert _vulnerable(combat.player) == 2


# ---------------------------------------------------------------------------
# `creature != Owner` -- only the owner's own death counts
# ---------------------------------------------------------------------------

def test_a_different_creature_dying_releases_nothing():
    combat, beast, other = _make_combat(extra_enemy=True)

    assert combat.kill_creature(other)

    assert _vulnerable(combat.player) == 0
    assert beast.has_power(PowerId.SPORE_CLOUD)

    # ...and the owner's own later death still works.
    assert combat.kill_creature(beast)
    assert _vulnerable(combat.player) == 2


def test_two_owners_each_release_their_own_cloud_once():
    combat, beast, _ = _make_combat(extra_enemy=True)
    second, second_ai = create_fungi_beast(Rng(99))
    combat.add_enemy(second, second_ai)
    # No hand-application: create_fungi_beast brings its own Spore Cloud 2,
    # and adding another would stack it to 4 and make the counts below wrong.
    assert second.get_power_amount(PowerId.SPORE_CLOUD) == 2

    combat.kill_creature(beast)
    assert _vulnerable(combat.player) == 2
    combat.kill_creature(second)
    assert _vulnerable(combat.player) == 4


# ---------------------------------------------------------------------------
# `wasRemovalPrevented` -- the AfterDeath timing detail
# ---------------------------------------------------------------------------

def test_prevented_removal_suppresses_the_cloud_and_keeps_the_power():
    """The whole point of AfterDeath: a death that gets undone releases nothing.

    Fairy in a Bottle is this simulator's only removal-prevention path
    (``CombatState._prevent_death_if_needed``). It heals the creature BEFORE
    ``after_death`` fires, so at hook time the owner is alive again and a
    ``BeforeDeath``-style port -- or one that ignored ``wasRemovalPrevented``
    -- would happily apply Vulnerable here. The .cs returns instead.
    """
    combat, beast, _ = _make_combat(extra_enemy=True)
    player = combat.player
    player.apply_power(PowerId.SPORE_CLOUD, 2)
    assert combat.add_potion(create_potion("FairyInABottle"))
    player.current_hp = 1

    assert combat.kill_creature(player)

    assert player.is_alive, "fairy should have prevented removal"
    assert _vulnerable(player) == 0
    # The power survives an undone death, so a later real death still fires.
    assert player.has_power(PowerId.SPORE_CLOUD)


def test_the_hook_returns_immediately_when_the_flag_is_set():
    # Direct call with exactly the signature CombatState.kill_creature uses,
    # for the enemy-owner case the simulator has no in-combat path to yet.
    combat, beast, _ = _make_combat(extra_enemy=True)
    power = beast.powers[PowerId.SPORE_CLOUD]

    power.after_death(beast, beast, combat, True)
    assert _vulnerable(combat.player) == 0

    power.after_death(beast, beast, combat, False)
    assert _vulnerable(combat.player) == 2


# ---------------------------------------------------------------------------
# `PlayerCreatures.Where(c => c.IsAlive)` -- who actually gets the spores
# ---------------------------------------------------------------------------

def test_player_side_allies_that_are_not_players_get_nothing():
    combat, beast, _ = _make_combat(extra_enemy=True)
    osty = combat.summon_osty(combat.player, 20)
    assert osty is not None and osty.is_alive
    assert not osty.is_player

    combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 2
    assert _vulnerable(osty) == 0


def test_every_living_player_gets_the_cloud_in_multiplayer():
    combat, beast, _ = _make_combat(extra_enemy=True)
    ally = combat.add_ally_player(
        PlayerState(player_id=2, character_id="Ironclad", max_hp=60, current_hp=60)
    )

    combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 2
    assert _vulnerable(ally) == 2


def test_dead_players_are_skipped():
    combat, beast, _ = _make_combat(extra_enemy=True)
    ally = combat.add_ally_player(
        PlayerState(player_id=2, character_id="Ironclad", max_hp=60, current_hp=60)
    )
    ally.current_hp = 0
    assert not ally.is_alive

    combat.kill_creature(beast)

    assert _vulnerable(combat.player) == 2
    assert _vulnerable(ally) == 0


def test_no_living_player_means_no_cloud_at_all():
    combat, beast, _ = _make_combat(extra_enemy=True)
    combat.player.current_hp = 0
    assert not combat.player.is_alive

    assert combat.kill_creature(beast)

    assert PowerId.VULNERABLE not in combat.player.powers
