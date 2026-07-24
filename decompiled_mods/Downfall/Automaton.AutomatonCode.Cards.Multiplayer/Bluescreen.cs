using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Cards.Multiplayer;

[Pool(typeof(AutomatonCardPool))]
public class Bluescreen : AutomatonCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Bluescreen()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)6)
	{
		((ConstructedCardModel)this).WithBlock(12, 5);
		((ConstructedCardModel)(object)this).WithTip<Error>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Creature target = cardPlay.Target;
		Player player = ((target != null) ? target.Player : null);
		if (player != null)
		{
			await CreatureCmd.GainBlock(player.Creature, ((DynamicVar)((CardModel)this).DynamicVars.Block).BaseValue, (ValueProp)8, cardPlay, false);
			await DownfallCardCmd.GiveCard<Error>(player, (PileType)1, (CardPilePosition)2, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Error>?)null, ((CardModel)this).Owner);
		}
	}
}
