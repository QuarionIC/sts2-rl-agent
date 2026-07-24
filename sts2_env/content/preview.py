"""Live effective-value previews for cards in combat.

Computes the numbers a card would *actually* produce right now — Strength,
Vulnerable, Weak, Frail, Dexterity, enchantments, relic hooks, cost-modifying
powers — by running the card's base values through the simulator's own
``modify_damage`` / ``modify_block`` pipelines (the exact code paths that
``calculate_damage`` / ``calculate_block`` reach on play) against the live
combat state. No combat formulas are reimplemented here.

A preview must never mutate combat state. The pipelines are *almost* pure,
but three side channels exist on the real play path:

* ``calculate_damage(..., combat)`` pushes a pending attack context and fires
  ``before_attack`` hooks (and flushing it fires ``after_attack`` hooks, which
  e.g. consume Vigor). The preview therefore calls ``hooks.modify_damage``
  directly — the same math, minus the attack-context bookkeeping.
* Several modifier hooks read ``combat.active_card_source`` (DoubleDamage,
  Gigantification, enchantments). The preview temporarily impersonates a card
  play by setting ``combat._active_card_source`` to the previewed card, and
  restores it afterwards.
* ``modify_block`` notifies modifiers via ``after_modifying_block_amount``
  (Vambrace / Pael's Legion record the triggering card). The preview
  snapshots and restores every power/relic instance's state around the
  computation so those recordings never leak.
"""

from __future__ import annotations

from contextlib import contextmanager

from sts2_env.core.enums import CardTag, TargetType, ValueProp

from sts2_env.content.descriptions import card_damage_clause


def _empty_preview(card) -> dict:
    return {
        "eff_cost": None,
        "base_cost": getattr(card, "cost", None),
        "eff_block": None,
        "base_block": getattr(card, "base_block", None),
        "base_damage": getattr(card, "base_damage", None),
        "hits": None,
        "eff_damage_by_target": [],
        "all_enemies": False,
        "targeted": False,
        "flags": {},
    }


def _iter_modifier_instances(combat):
    """Yield every power/relic instance whose hooks the pipelines may call."""
    for creature in combat.all_creatures:
        yield from creature.powers.values()
    player_states = getattr(combat, "combat_player_states", None)
    if player_states:
        for state in player_states:
            yield from getattr(state, "relics", ())
    else:
        yield from getattr(combat, "relics", ())


@contextmanager
def _preview_guard(combat, card):
    """Impersonate a play of ``card`` without letting any state leak."""
    snapshots = [
        (instance, dict(instance.__dict__))
        for instance in _iter_modifier_instances(combat)
    ]
    saved_source = combat._active_card_source  # noqa: SLF001
    combat._active_card_source = card  # noqa: SLF001
    try:
        yield
    finally:
        combat._active_card_source = saved_source  # noqa: SLF001
        for instance, snapshot in snapshots:
            instance.__dict__.clear()
            instance.__dict__.update(snapshot)


def _damage_dealer(card, combat, owner):
    """The creature whose powers apply to this card's damage (matches play).

    Osty-attack cards are dealt by the owner's Osty (see the card effects'
    ``_deal_osty_damage_*`` helpers); everything else by the owner.
    """
    if CardTag.OSTY_ATTACK in getattr(card, "tags", frozenset()):
        osty = combat.get_osty(owner)
        if osty is None or not osty.is_alive:
            return None
        return osty
    return owner


def _damage_base(card) -> int | None:
    """Per-hit base damage as the play path reads it."""
    if CardTag.OSTY_ATTACK in getattr(card, "tags", frozenset()):
        return card.effect_vars.get("osty_damage", card.base_damage)
    return card.base_damage


def _card_preview(card, combat, owner) -> dict:
    from sts2_env.core.hooks import modify_block, modify_damage

    owner = owner or getattr(card, "owner", None) or combat.primary_player
    preview = _empty_preview(card)
    flags: dict[str, bool] = {}

    # ── Effective energy cost (BorrowedTime, Snecko-style powers, relics) ──
    if not card.has_energy_cost_x and not card.is_unplayable:
        base_cost = max(0, card.cost)
        eff_cost = combat.modified_card_cost(owner, card)
        preview["base_cost"] = base_cost
        preview["eff_cost"] = eff_cost
        if eff_cost > base_cost:
            flags["cost_up"] = True
        elif eff_cost < base_cost:
            flags["cost_down"] = True

    clause = card_damage_clause(card)
    base_damage = _damage_base(card)
    target_type = card.target_type_for(owner)
    preview["targeted"] = target_type == TargetType.ANY_ENEMY
    preview["all_enemies"] = bool(
        (clause or {}).get("all_enemies") or target_type == TargetType.ALL_ENEMIES
    )

    base_block = card.base_block

    if base_damage is None and base_block is None:
        preview["flags"] = flags
        return preview

    with _preview_guard(combat, card):
        # ── Effective damage, per living enemy (Vulnerable/Weak differ) ──
        if base_damage is not None and clause is not None:
            preview["base_damage"] = base_damage
            preview["hits"] = clause["hits"]
            props = ValueProp.MOVE
            if clause["non_attack"]:
                props |= ValueProp.UNPOWERED
            dealer = _damage_dealer(card, combat, owner)
            if dealer is not None:
                for index, enemy in enumerate(combat.enemies):
                    if not enemy.is_alive:
                        continue
                    value = modify_damage(
                        base_damage, dealer, enemy, props, combat, card_source=card,
                    )
                    preview["eff_damage_by_target"].append({
                        "enemy_index": index,
                        "value": value,
                        "hits": clause["hits"],
                    })
                values = [entry["value"] for entry in preview["eff_damage_by_target"]]
                if any(value > base_damage for value in values):
                    flags["damage_up"] = True
                if any(value < base_damage for value in values):
                    flags["damage_down"] = True

        # ── Effective block on the owner (Frail/Dexterity/relics) ──
        if base_block is not None:
            eff_block = modify_block(
                base_block, owner, ValueProp.MOVE, combat,
                card_source=card, card_play=None,
            )
            preview["eff_block"] = eff_block
            if eff_block > base_block:
                flags["block_up"] = True
            elif eff_block < base_block:
                flags["block_down"] = True

    preview["flags"] = flags
    return preview


def card_preview(card, combat, owner=None) -> dict:
    """Return the values ``card`` would produce if played right now.

    ``{eff_cost, base_cost, eff_block, base_block, base_damage, hits,
    eff_damage_by_target: [{enemy_index, value, hits}], all_enemies, targeted,
    flags: {damage_up/damage_down/block_up/block_down/cost_up/cost_down}}``

    * Damage/block are computed through the simulator's own modifier
      pipelines against the live combat, so they match what ``play_card``
      would deal exactly (per-hit; special per-card scaling such as X-cost
      repetition stays per-hit).
    * ``eff_damage_by_target`` holds one entry per living enemy — targeted
      attacks genuinely differ per enemy (Vulnerable, etc.).
    * Cards without damage/block just report costs. Never raises, never
      mutates combat state.
    """
    try:
        return _card_preview(card, combat, owner)
    except Exception:
        return _empty_preview(card)
