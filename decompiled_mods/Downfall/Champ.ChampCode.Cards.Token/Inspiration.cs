using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Champ.ChampCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Inspiration : ChampCardModel
{
	public Inspiration()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.EnterDifferentStance(ctx, ((CardModel)this).Owner);
	}
}
