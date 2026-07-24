using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class StasisField : GuardianCardModel
{
	public StasisField()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(6, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await GuardianCmd.PutIntoStasis((CardModel)(object)this, ctx, (AbstractModel)(object)this);
	}
}
