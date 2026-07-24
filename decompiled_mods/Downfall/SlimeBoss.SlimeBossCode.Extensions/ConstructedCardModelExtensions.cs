using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SlimeBoss.SlimeBossCode.DynamicVars;

namespace SlimeBoss.SlimeBossCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithSlurp(this ConstructedCardModel card, decimal baseVal, decimal upgradedVal = 0m)
	{
		card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<SlurpVar>(new SlurpVar(baseVal), upgradedVal));
		return card;
	}

	public static ConstructedCardModel WithCommand(this ConstructedCardModel card, decimal baseVal, decimal upgradedVal = 0m)
	{
		card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<CommandVar>(new CommandVar(baseVal), upgradedVal));
		return card;
	}
}
