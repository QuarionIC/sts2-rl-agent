using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Crusher : AwakenedCardModel
{
	public Crusher()
		: base(5, (CardType)1, (CardRarity)7, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(25, 5);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? player)
	{
		if (card.Owner != ((CardModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		((CardModel)this).EnergyCost.AddUntilPlayed(-1, false);
		return Task.CompletedTask;
	}
}
