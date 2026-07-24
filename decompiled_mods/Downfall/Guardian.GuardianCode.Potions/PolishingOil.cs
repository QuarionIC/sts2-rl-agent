using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Potions;

[Pool(typeof(GuardianPotionPool))]
public class PolishingOil : GuardianPotionModel
{
	public PolishingOil()
		: base((PotionRarity)2, (PotionUsage)1, (TargetType)1)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.Polish);
		WithVars(new PolishVar(5));
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return GuardianCmd.Polish(ctx, (AbstractModel)(object)this);
	}
}
