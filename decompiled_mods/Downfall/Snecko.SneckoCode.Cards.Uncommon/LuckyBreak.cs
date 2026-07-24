using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class LuckyBreak : SneckoCardModel
{
	private int TwoCostInHand => ((CardModel)this).Owner.GetHand().Count((CardModel e) => (decimal)e.EnergyCost.GetResolved() >= ((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue);

	public LuckyBreak()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(8, 3);
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithEnergy(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CardPileCmd.Draw(ctx, (decimal)TwoCostInHand, ((CardModel)this).Owner, false);
	}
}
