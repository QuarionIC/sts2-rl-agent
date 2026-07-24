using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Guardian.GuardianCode.Cards.Token;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class GuardianGear : GuardianRelicModel, IAfterGuardianModeChange
{
	public GuardianGear()
		: base((RelicRarity)1)
	{
	}

	public async Task AfterGuardianModeChange(PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		if (player == ((RelicModel)this).Owner && newMode is GuardianDefensiveMode)
		{
			((RelicModel)this).Flash();
			await PlayerCmd.GainEnergy(1m, player);
			await CardPileCmd.Draw(ctx, 2m, player, false);
		}
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await DownfallCardCmd.GiveCard<GearUp>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<GearUp>?)null, (Player?)null);
			}
		}
	}
}
