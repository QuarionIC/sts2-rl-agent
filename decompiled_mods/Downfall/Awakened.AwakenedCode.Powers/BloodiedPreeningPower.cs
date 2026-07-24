using System;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Powers;

public class BloodiedPreeningPower : AwakenedPowerModel
{
	public BloodiedPreeningPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<PlumeJab>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await DownfallCardCmd.GiveCards<PlumeJab>(player, (PileType)2, (decimal)((PowerModel)this).Amount, (CardPilePosition)1, upgraded: false, 0.1f, (CardPreviewStyle)1, skipAnimation: false, (Action<PlumeJab>?)null, (Player?)null);
			((PowerModel)this).Flash();
		}
	}
}
