using System;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Powers;

public class ThaumaturgyPower : AwakenedPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await DownfallCardCmd.GiveCard<Ceremony>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.1f, (CardPreviewStyle)1, skipAnimation: false, (Action<Ceremony>?)null, (Player?)null);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public ThaumaturgyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
