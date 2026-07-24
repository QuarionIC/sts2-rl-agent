using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Hooks;

public readonly record struct HealthBarForecastContext(Creature Creature)
{
	public CombatStateWrapper? CombatState => BetaMainCompatibility.Creature_.WrappedCombatState(Creature);

	public CombatSide? CurrentSide => CombatState?.CurrentSide;
}
