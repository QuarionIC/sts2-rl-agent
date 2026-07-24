using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class WorthySacrifice : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public WorthySacrifice()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> cards = (await DownfallCardCmd.SelectFromHand(ctx, CardSelectorPrefs.ExhaustSelectionPrompt, (CardModel)(object)this)).ToList();
		foreach (CardModel item in cards)
		{
			await CardCmd.Exhaust(ctx, item, false, false);
		}
		await TransformCards(cards, (CardType)1, (CardType)2);
		await TransformCards(cards, (CardType)2, (CardType)1);
	}

	private async Task TransformCards(List<CardModel> cards, CardType from, CardType to)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		int num = cards.Count((CardModel e) => e.Type == from);
		if (num == 0)
		{
			return;
		}
		IEnumerable<CardModel> enumerable = from c in ((CardModel)this).Owner.Character.CardPool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where c.Type == to
			select c;
		List<CardModel> list = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, enumerable, num, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).ToList();
		if (((CardModel)this).IsUpgraded)
		{
			foreach (CardModel item in list.Where((CardModel card) => card.IsUpgradable))
			{
				CardCmd.Upgrade(item, (CardPreviewStyle)1);
			}
		}
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}
