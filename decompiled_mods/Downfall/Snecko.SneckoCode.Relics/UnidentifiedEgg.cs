using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class UnidentifiedEgg : SneckoRelicModel
{
	public override bool HasUponPickupEffect => true;

	public UnidentifiedEgg()
		: base((RelicRarity)4)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		WithVars((DynamicVar)new CardsVar(2));
	}

	public override Task AfterObtained()
	{
		foreach (CardModel item in ListExtensions.StableShuffle<CardModel>(PileTypeExtensions.GetPile((PileType)6, ((RelicModel)this).Owner).Cards.Where((CardModel c) => SneckoCmd.IsOffclass(c) && c.IsUpgradable).ToList(), ((RelicModel)this).Owner.RunState.Rng.Niche).Take(((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue))
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
		}
		return Task.CompletedTask;
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (player != ((RelicModel)this).Owner || ((Enum)options.Flags).HasFlag((Enum)(object)(CardCreationFlags)4))
		{
			return false;
		}
		UpgradeValidCards(cardRewards, SneckoCmd.IsOffclass, (RelicModel)(object)this);
		return true;
	}

	public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
	{
		if (player == ((RelicModel)this).Owner)
		{
			UpgradeValidCards(cards, SneckoCmd.IsOffclass, (RelicModel)(object)this);
		}
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (card.Owner != ((RelicModel)this).Owner || !SneckoCmd.IsOffclass(card) || !card.IsUpgradable)
		{
			return false;
		}
		newCard = ((ICardScope)((RelicModel)this).Owner.RunState).CloneCard(card);
		CardCmd.Upgrade(newCard, (CardPreviewStyle)0);
		return true;
	}

	private static void UpgradeValidCards(List<CardCreationResult> cards, Func<CardModel, bool> filter, RelicModel eggRelic)
	{
		foreach (CardCreationResult card2 in cards)
		{
			CardModel card = card2.Card;
			if (filter(card) && card.IsUpgradable)
			{
				CardModel val = ((ICardScope)eggRelic.Owner.RunState).CloneCard(card);
				CardCmd.Upgrade(val, (CardPreviewStyle)1);
				card2.ModifyCard(val, eggRelic);
			}
		}
	}
}
