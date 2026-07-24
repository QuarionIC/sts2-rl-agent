using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class PunchTicket : AutomatonCardModel
{
	public PunchTicket()
		: base(0, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
		((ConstructedCardModel)(object)this).WithTip<Error>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await DownfallCardCmd.GiveCard<Error>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Error>?)null, (Player?)null);
	}
}
