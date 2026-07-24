using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Potions;

[Pool(typeof(GuardianPotionPool))]
public class LiquidAccelerant : GuardianPotionModel
{
	public LiquidAccelerant()
		: base((PotionRarity)1, (PotionUsage)1, (TargetType)1)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.Accelerate);
		WithTip(GuardianTip.Stasis);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return GuardianCmd.AccelerateUntilExit(ctx, ((PotionModel)this).Owner);
	}
}
