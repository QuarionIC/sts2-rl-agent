using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Ancient;

[Pool(typeof(AutomatonCardPool))]
public class FlawlessArtifact : AutomatonCardModel
{
	public FlawlessArtifact()
		: base(0, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)this).WithUpgradingCardTip<Constructor>((Action<Constructor, CardModel>)null);
		((ConstructedCardModel)this).WithUpgradingCardTip<Separator>((Action<Separator, CardModel>)null);
		((ConstructedCardModel)this).WithUpgradingCardTip<Terminator>((Action<Terminator, CardModel>)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await DownfallCardCmd.GiveCard<Constructor>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Constructor>?)null, (Player?)null);
		await DownfallCardCmd.GiveCard<Separator>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Separator>?)null, (Player?)null);
		await DownfallCardCmd.GiveCard<Terminator>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Terminator>?)null, (Player?)null);
	}
}
