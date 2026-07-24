using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class BugBarrage : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public BugBarrage()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)(object)this).WithTip<Error>();
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await DownfallCardCmd.GiveCards<Error>(((CardModel)this).Owner, (PileType)2, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Error>?)null, (Player?)null);
		IReadOnlyList<CardModel> hand = ((CardModel)this).Owner.GetHand((CardModel c) => (int)c.Type == 4);
		int count = hand.Count;
		await CardCmd.DiscardAndDraw(ctx, (IEnumerable<CardModel>)hand, count);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, count, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
