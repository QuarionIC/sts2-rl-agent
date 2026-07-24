using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class ShieldSpikes : GuardianCardModel
{
	public ShieldSpikes()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(12, 4);
		((ConstructedCardModel)this).WithPower<ThornsPower>(3, 1);
		((ConstructedCardModel)(object)this).WithBrace(8);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (GuardianCmd.IsInMode<GuardianDefensiveMode>(((CardModel)this).Owner))
		{
			await CommonActions.ApplySelf<ThornsPower>(ctx, (CardModel)(object)this, false);
		}
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
