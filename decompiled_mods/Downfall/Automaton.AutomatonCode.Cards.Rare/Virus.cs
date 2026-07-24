using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class Virus : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public Virus()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithUpgradingCardTip<MinorBeam>((Action<MinorBeam, CardModel>)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		IReadOnlyList<CardModel> hand = ((CardModel)this).Owner.GetHand();
		int size = hand.Count;
		await CardCmd.Discard(ctx, (IEnumerable<CardModel>)hand);
		await DownfallCardCmd.GiveCards<MinorBeam>(((CardModel)this).Owner, (PileType)2, (decimal)size, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<MinorBeam>?)null, (Player?)null);
	}
}
