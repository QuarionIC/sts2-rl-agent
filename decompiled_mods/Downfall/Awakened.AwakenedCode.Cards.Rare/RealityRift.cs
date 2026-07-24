using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class RealityRift : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	private static CardModel[] AnotherDimensionCards => (CardModel[])(object)new CardModel[9]
	{
		(CardModel)ModelDb.Card<Crusher>(),
		(CardModel)ModelDb.Card<Daggerstorm>(),
		(CardModel)ModelDb.Card<ManaShield>(),
		(CardModel)ModelDb.Card<Minniegun>(),
		(CardModel)ModelDb.Card<Mantis>(),
		(CardModel)ModelDb.Card<Scheme>(),
		(CardModel)ModelDb.Card<SignInBlood>(),
		(CardModel)ModelDb.Card<SpreadingSpores>(),
		(CardModel)ModelDb.Card<TheEncyclopedia>()
	};

	public RealityRift()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithTip<Void>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await DownfallCardCmd.GiveCard<Void>(((CardModel)this).Owner, (PileType)1, (CardPilePosition)2, upgraded: false, 0.2f, (CardPreviewStyle)1, skipAnimation: false, (Action<Void>?)null, (Player?)null);
		List<CardModel> list = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, (IEnumerable<CardModel>)AnotherDimensionCards, 3, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).ToList();
		if (((CardModel)this).IsUpgraded)
		{
			CardCmd.Upgrade((IEnumerable<CardModel>)list, (CardPreviewStyle)1);
		}
		CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, true);
		if (val != null)
		{
			await CardPileCmd.AddGeneratedCardToCombat(val, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
		}
	}
}
