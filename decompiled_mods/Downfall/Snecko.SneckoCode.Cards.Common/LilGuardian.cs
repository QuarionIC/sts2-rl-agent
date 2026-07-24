using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class LilGuardian : SneckoCardModel
{
	public LilGuardian()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 2);
		((ConstructedCardModel)this).WithEnergy(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != ((CardModel)this).Owner)
		{
			return;
		}
		ResourceInfo resources = cardPlay.Resources;
		if (!((decimal)((ResourceInfo)(ref resources)).EnergySpent < ((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue))
		{
			CardPile pile = ((CardModel)this).Pile;
			if (pile != null && (int)pile.Type == 2)
			{
				await CardCmd.AutoPlay(ctx, (CardModel)(object)this, (Creature)null, (AutoPlayType)1, false, false);
			}
		}
	}
}
