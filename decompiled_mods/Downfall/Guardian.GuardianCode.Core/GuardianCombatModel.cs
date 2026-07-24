using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Displays;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Piles;
using Guardian.GuardianCode.Powers;
using Guardian.GuardianCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Guardian.GuardianCode.Core;

public class GuardianCombatModel : CustomSingletonModel
{
	internal static readonly SpireField<Player, GuardianModeModel> ActiveMode = new SpireField<Player, GuardianModeModel>((Func<GuardianModeModel>)GuardianModelDb.GuardianMode<GuardianNormalMode>);

	internal static readonly SpireField<Player, int> StasisSlots = new SpireField<Player, int>((Func<int>)(() => -1));

	internal static readonly SpireField<CardModel, int> StasisCounter = new SpireField<CardModel, int>((Func<CardModel, int>)((CardModel _) => 0));

	public GuardianCombatModel()
		: base((HookType)1)
	{
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player != null && player.Character is Guardian)
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await PowerCmd.Apply<ModeShiftPower>(ctx, player.Creature, 20m, player.Creature, (CardModel)null, true);
				await GuardianCmd.LeaveDefensiveMode(ctx, player);
			}
		}
		await GuardianCmd.TickAll(player, ctx);
		GuardianDisplay.Refresh(player);
	}

	public override Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (card.Pile != null && card.Pile.Type != GuardianPile.Stasis)
		{
			return Task.CompletedTask;
		}
		GuardianDisplay.Refresh(card.Owner);
		return Task.CompletedTask;
	}

	internal static void SetupGuardianCombatUi(CombatState state)
	{
		if (NCombatRoom.Instance == null)
		{
			return;
		}
		foreach (Player player in state.Players)
		{
			StasisSlots.Set(player, -1);
		}
		foreach (Player player2 in state.Players)
		{
			if (player2.Character is Guardian)
			{
				InitStasisUi(player2);
			}
		}
	}

	internal static GuardianPile GetOrInitStasis(Player player)
	{
		GuardianPile stasisPile = GuardianCmd.GetStasisPile(player);
		InitStasisUi(player);
		return stasisPile;
	}

	internal static void InitStasisUi(Player player)
	{
		if (StasisSlots[player] < 0)
		{
			StasisSlots.Set(player, (!(player.Character is Guardian)) ? 1 : 3);
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null && !GuardianDisplay.HasDisplay(player))
		{
			GuardianDisplay.SetupGuardianUi(instance, player);
		}
		GuardianDisplay.Refresh(player);
	}

	internal static async Task SetMode(PlayerChoiceContext ctx, Player player, GuardianModeModel newCanonical)
	{
		GuardianModeModel current = ActiveMode[player];
		if (!(((object)current)?.GetType() == ((object)newCanonical).GetType()))
		{
			if (current != null)
			{
				await current.OnExit();
			}
			GuardianModeModel guardianModeModel = newCanonical.ToMutable(player);
			ActiveMode[player] = guardianModeModel;
			await guardianModeModel.OnEnter();
			await Cmd.Wait(0.2f, false);
			TriggerModeAnimation(player);
			await Cmd.Wait(0.2f, false);
			await GuardianHook.AfterGuardianModeChangeEarly(player.Creature.CombatState, ctx, player, current, ActiveMode[player]);
			await GuardianHook.AfterGuardianModeChange(player.Creature.CombatState, ctx, player, current, ActiveMode[player]);
		}
	}

	private static void TriggerModeAnimation(Player player)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature obj = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
		if (((obj != null) ? obj.Visuals : null) is NGuardianCreatureVisuals nGuardianCreatureVisuals)
		{
			nGuardianCreatureVisuals.IsDefensive = ActiveMode[player] is GuardianDefensiveMode;
			nGuardianCreatureVisuals.OnAnimationTrigger("Idle");
		}
	}
}
