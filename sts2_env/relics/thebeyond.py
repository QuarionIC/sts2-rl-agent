""""Acts from the Past" mod -- TheBeyond legacy-act event relics.

Decompiled reference: decompiled_mods/ActsFromThePast/ActsFromThePast.Relics/
MarkOfTheBloom.cs ([Pool(EventRelicPool)], C# RelicRarity 6 == Event).
Granted only by TheBeyond's MindBloom event ("I am Awake") -- never rolled
from the normal relic grab bag (RelicPool.EVENT is excluded by
PlayerState.populate_relic_grab_bag). Mirrors sts2_env/relics/exordium.py /
thecity.py's per-legacy-act relic module convention.

GoldenIdol (consumed by TheBeyond's MoaiHead "Offer Gold Idol" option) lives
in sts2_env/relics/exordium.py.
"""

from __future__ import annotations

from sts2_env.relics.base import RelicId, RelicInstance, RelicPool, RelicRarity
from sts2_env.relics.registry import register_relic


@register_relic
class MarkOfTheBloom(RelicInstance):
    """You can no longer heal.

    C# ref: MarkOfTheBloom implements IHealAmountModifier;
    ModifyHealMultiplicative returns 0 for the owner (all healing reduced to
    zero for the rest of the run). The duck-typed ``modify_heal_amount`` hook
    below is consulted by both Creature.heal (in combat -- via
    CombatState.relics_for_creature) and PlayerState.heal (out of combat --
    rest sites, event heals, potion heals), so every heal pipeline in the
    simulator flows through it.

    NOTE: the sim has no FairyInABottle-style revive interception; the C#
    heal-multiplier applies to that too, but revives set HP directly and are
    out of scope here.
    """

    relic_id = RelicId.MARK_OF_THE_BLOOM
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT

    def modify_heal_amount(self, owner: object, amount: int) -> int:
        return 0
