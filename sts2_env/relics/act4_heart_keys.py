"""The three Act4Heart keys (Emerald/Ruby/Sapphire).

C# ref: decompiled_mods/Act4Heart/Act4Heart.Keys/{EmeraldKey,RubyKey,
SapphireKey,KeyRelicModel}.cs.

These relics have no combat/reward hooks of their own -- ``KeyRelicModel``
in the C# source is an empty base class. All of the interesting behavior
(how each key is obtained, the Super Elite buff, the Recall rest-site
option, the Act 3->4 gate) lives in ``run/modifiers.py``'s
``Act4HeartModifier`` and ``run/rest_site.py``'s ``RecallOption`` --
mirroring how the mod itself splits "the relic" from "the ModelHook that
grants it" (GreenKeyHooks.cs / RedKeyHooks.cs / BlueKeyHooks.cs).

Rarity ANCIENT + pool EVENT together guarantee these are never offered by
normal shop/reward/boss-relic rolls (``PlayerState.populate_relic_grab_bag``
only draws from COMMON/UNCOMMON/RARE/SHOP rarities) -- they can only be
obtained via their specific trigger.
"""

from __future__ import annotations

from sts2_env.core.enums import RelicRarity
from sts2_env.relics.base import RelicId, RelicPool, RelicInstance
from sts2_env.relics.registry import register_relic


class KeyRelicModel(RelicInstance):
    """Shared base for the three Act4Heart key relics."""

    rarity = RelicRarity.ANCIENT
    pool = RelicPool.EVENT


@register_relic
class EmeraldKey(KeyRelicModel):
    """Obtained by killing the act's secretly pre-selected Super Elite.

    See ``Act4HeartModifier`` in ``run/modifiers.py`` for the elite
    pre-selection (seeded by run seed + act index) and buff-application
    logic.
    """
    relic_id = RelicId.EMERALD_KEY


@register_relic
class RubyKey(KeyRelicModel):
    """Obtained by choosing "Recall" at a Rest Site.

    See ``RecallOption`` in ``run/rest_site.py``.
    """
    relic_id = RelicId.RUBY_KEY


@register_relic
class SapphireKey(KeyRelicModel):
    """Obtained by skipping a Treasure Room's relic reward.

    See ``RunManager._do_treasure_skip`` in ``run/run_manager.py``.
    """
    relic_id = RelicId.SAPPHIRE_KEY
