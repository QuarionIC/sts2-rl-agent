using System;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Powers;

public sealed class FatalDesirePower : HermitPowerModel
{
	public FatalDesirePower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			((PowerModel)this).Flash();
			await DownfallCardCmd.GiveCards<Injury>(((PowerModel)this).Owner.Player, (PileType)2, (decimal)((PowerModel)this).Amount, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Injury>?)null, (Player?)null);
		}
	}
}
