using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.DynamicVars;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Guardian.GuardianCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithAccelerate(this ConstructedCardModel card, int baseVal, int upgradeVal = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		card.WithTip(TooltipSource.op_Implicit(GuardianTip.Accelerate), baseVal, upgradeVal);
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<AccelerateVar>(new AccelerateVar(baseVal), (decimal)upgradeVal) });
	}

	public static ConstructedCardModel WithBrace(this ConstructedCardModel card, int baseVal, int upgradeVal = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		card.WithTip(TooltipSource.op_Implicit(GuardianTip.Brace), baseVal, upgradeVal);
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<BraceVar>(new BraceVar(baseVal), (decimal)upgradeVal) });
	}

	public static ConstructedCardModel WithPolish(this ConstructedCardModel card, int baseVal, int upgradeVal = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		card.WithTip(TooltipSource.op_Implicit(GuardianTip.Polish));
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<PolishVar>(new PolishVar(baseVal), (decimal)upgradeVal) });
	}
}
