using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class LoneWolf : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public LoneWolf()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		CardPile handPile = PileTypeExtensions.GetPile((PileType)2, ((CardModel)this).Owner);
		if (handPile.Cards.Count == 0)
		{
			return;
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(((CardModel)this).SelectionScreenPrompt, 1);
		CardModel chosen = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		if (chosen != null)
		{
			chosen.SetToFreeThisTurn();
			await CardPileCmd.Add((IEnumerable<CardModel>)handPile.Cards.Where((CardModel c) => c != chosen).ToList(), (PileType)3, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
