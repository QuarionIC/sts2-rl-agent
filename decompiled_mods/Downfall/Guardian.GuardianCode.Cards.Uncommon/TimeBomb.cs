using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class TimeBomb : GuardianCardModel
{
	public TimeBomb()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(14, 4);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithAccelerate(1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await GuardianCmd.Accelerate(ctx, (AbstractModel)(object)this, AccelerateType.All);
	}
}
