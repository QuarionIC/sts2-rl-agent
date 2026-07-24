using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class FourthDimension : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public override bool CanBeGeneratedInCombat => false;

	public FourthDimension()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCards(3, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel card = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		if (card != null)
		{
			List<CardModel> list = new List<CardModel>();
			for (int i = 0; i < ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue; i++)
			{
				list.Add(card.CreateClone());
			}
			IReadOnlyList<CardPileAddResult> a = await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)1, ((CardModel)this).Owner, (CardPilePosition)3);
			await CardCmd.Exhaust(ctx, card, false, false);
			CardCmd.PreviewCardPileAdd(a, 0.1f, (CardPreviewStyle)2);
		}
	}
}
