using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class LuckOfTheDraw : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public LuckOfTheDraw()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithVar("Threshold", 3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		int threshold = ((CardModel)this).DynamicVars["Threshold"].IntValue;
		CardModel val;
		for (int totalCost = 0; totalCost < threshold; totalCost += val.EnergyCost.GetWithModifiers((CostModifiers)2))
		{
			if (((CardModel)this).Owner.GetHand().Count >= CardPile.MaxCardsInHand)
			{
				break;
			}
			List<CardModel> list = (await CardPileCmd.Draw(ctx, 1m, ((CardModel)this).Owner, false)).ToList();
			if (list.Count != 0)
			{
				val = list.First();
				continue;
			}
			break;
		}
	}
}
