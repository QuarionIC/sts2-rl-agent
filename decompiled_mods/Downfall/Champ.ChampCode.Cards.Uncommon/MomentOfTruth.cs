using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class MomentOfTruth : ChampCardModel
{
	public MomentOfTruth()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)(object)this).WithFinisher();
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
