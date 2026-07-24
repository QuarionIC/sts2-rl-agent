using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Hooks;

public static class HealthBarForecastOrder
{
	public static int ForSideTurnStart(Creature creature, CombatSide triggerSide)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		ArgumentNullException.ThrowIfNull(creature, "creature");
		CombatStateWrapper? combatStateWrapper = BetaMainCompatibility.Creature_.WrappedCombatState(creature);
		return (combatStateWrapper != null && combatStateWrapper.CurrentSide == triggerSide) ? 1 : 0;
	}

	public static int ForSideTurnEnd(Creature creature, CombatSide triggerSide)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		ArgumentNullException.ThrowIfNull(creature, "creature");
		CombatStateWrapper? combatStateWrapper = BetaMainCompatibility.Creature_.WrappedCombatState(creature);
		return (combatStateWrapper == null || combatStateWrapper.CurrentSide != triggerSide) ? 1 : 0;
	}
}
