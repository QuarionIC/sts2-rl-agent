using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Automaton.AutomatonCode.DynamicVars;

public class StashVar : DynamicVar
{
	public StashVar(decimal value)
		: base("Stash", value)
	{
		DynamicVarExtensions.WithTooltip<StashVar>(this, (string)null, "static_hover_tips");
	}
}
