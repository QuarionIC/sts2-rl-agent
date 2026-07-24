using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Clutch : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowRedInternal => !Has0CostInDraw;

	private bool Has0CostInDraw => PileTypeExtensions.GetPile((PileType)1, ((CardModel)this).Owner).Cards.Any(delegate(CardModel c)
	{
		CardEnergyCost energyCost = c.EnergyCost;
		return energyCost != null && energyCost.Canonical == 0 && !energyCost.CostsX;
	});

	public Clutch()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithEnergyTip();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel val = ((IEnumerable<CardModel>)PileTypeExtensions.GetPile((PileType)1, ((CardModel)this).Owner).Cards).FirstOrDefault((Func<CardModel, bool>)delegate(CardModel c)
		{
			CardEnergyCost energyCost = c.EnergyCost;
			return energyCost != null && energyCost.Canonical == 0 && !energyCost.CostsX;
		});
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
