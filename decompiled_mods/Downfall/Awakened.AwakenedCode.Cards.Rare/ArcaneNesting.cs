using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class ArcaneNesting : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ArcaneNesting()
		: base(-1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)4 });
		((ConstructedCardModel)this).WithBlock(4, 2);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (((CardModel)this).Pile != null && cardPlay.Card.Owner == ((CardModel)this).Owner && (int)((CardModel)this).Pile.Type == 2 && (int)cardPlay.Card.Type == 3)
		{
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		}
	}
}
