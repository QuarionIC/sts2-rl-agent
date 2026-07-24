using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class TimeCapacitor : GuardianCardModel
{
	public TimeCapacitor()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithVar("StasisSlots", 1, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		GuardianCmd.AddMaxStasisSlots(((CardModel)this).Owner, ((CardModel)this).DynamicVars["StasisSlots"].IntValue);
		return Task.CompletedTask;
	}
}
