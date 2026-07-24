using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.DynamicVars;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class BaalorsLordlyPlate : GuardianRelicModel, IModifyBraceAmount
{
	public BaalorsLordlyPlate()
		: base((RelicRarity)2)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.Brace);
		WithVars(new BraceVar(1));
	}

	public decimal ModifyBraceAmount(Player player, decimal amount)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return amount;
		}
		return amount + ((DynamicVar)((RelicModel)this).DynamicVars.Brace()).BaseValue;
	}
}
