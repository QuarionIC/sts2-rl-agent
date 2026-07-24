using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Godot;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Displays;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Interfaces;
using Guardian.GuardianCode.Piles;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Guardian.GuardianCode.Core;

public static class GuardianCmd
{
	private static readonly LocString FullStasisText = new LocString("combat_messages", "FULL_STASIS_SLOTS");

	public static Task EnterDefensiveMode(PlayerChoiceContext ctx, Player player)
	{
		return GuardianCombatModel.SetMode(ctx, player, GuardianModelDb.GuardianMode<GuardianDefensiveMode>());
	}

	public static Task LeaveDefensiveMode(PlayerChoiceContext ctx, Player player)
	{
		return GuardianCombatModel.SetMode(ctx, player, GuardianModelDb.GuardianMode<GuardianNormalMode>());
	}

	public static bool IsInMode<T>(Player player) where T : GuardianModeModel
	{
		return GuardianCombatModel.ActiveMode[player] is T;
	}

	public static GuardianPile GetStasisPile(Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return (GuardianPile)(object)(CustomPiles.GetCustomPile(player.PlayerCombatState, GuardianPile.Stasis) ?? throw new ArgumentNullException("pile"));
	}

	public static int GetMaxStasisSlots(Player player)
	{
		return GuardianCombatModel.StasisSlots[player];
	}

	public static void AddMaxStasisSlots(Player player, int value = 1)
	{
		if (value > 0)
		{
			GuardianCombatModel.InitStasisUi(player);
			SpireField<Player, int> stasisSlots = GuardianCombatModel.StasisSlots;
			stasisSlots[player] += value;
			GuardianDisplay.Refresh(player);
		}
	}

	public static void RemoveMaxStasisSlots(Player player, int value = 1)
	{
		if (value > 0)
		{
			GuardianCombatModel.InitStasisUi(player);
			GuardianCombatModel.StasisSlots[player] = Math.Max(0, GuardianCombatModel.StasisSlots[player] - value);
			GuardianDisplay.Refresh(player);
		}
	}

	public static bool CanPutIntoStasis(Player player, Player? askingPlayer = null, bool silent = false)
	{
		if (askingPlayer == null)
		{
			askingPlayer = player;
		}
		if (((CardPile)GuardianCombatModel.GetOrInitStasis(player)).Cards.Count < GetMaxStasisSlots(player))
		{
			return true;
		}
		if (silent || !LocalContext.IsMe(askingPlayer))
		{
			return false;
		}
		ThinkCmd.Play(FullStasisText, player.Creature, 2.0);
		return false;
	}

	public static async Task<bool> PutIntoStasis(CardModel card, PlayerChoiceContext ctx, AbstractModel source, bool silent = false)
	{
		ICombatState cs = source.GetCreature().CombatState;
		if (cs == null)
		{
			return false;
		}
		Player owner = card.Owner;
		GuardianPile pile = GuardianCombatModel.GetOrInitStasis(owner);
		if (((CardPile)pile).Cards.Count >= GetMaxStasisSlots(owner))
		{
			if (!silent && LocalContext.IsMe(owner))
			{
				ThinkCmd.Play(FullStasisText, owner.Creature, 2.0);
			}
			return false;
		}
		await GuardianHook.BeforeCardEntersStasis(cs, ctx, card, source);
		await CardPileCmd.Add(card, (CardPile)(object)pile, (CardPilePosition)1, (AbstractModel)null, silent);
		SetStasisCounter(card);
		await GuardianHook.AfterCardEntersStasis(cs, ctx, card, source);
		return true;
	}

	public static int GetStasisCounter(CardModel card)
	{
		return GuardianCombatModel.StasisCounter[card];
	}

	public static void SetStasisCounter(CardModel card)
	{
		GuardianCombatModel.StasisCounter[card] = CalculateStasisCounter(card);
		GuardianDisplay.Refresh(card.Owner);
	}

	private static int CalculateStasisCounter(CardModel card)
	{
		if (card is ICustomTickDuration customTickDuration)
		{
			return customTickDuration.TickDuration;
		}
		if (card.EnergyCost.CostsX)
		{
			return card.Owner.PlayerCombatState.Energy + 1;
		}
		return card.EnergyCost.GetResolved() + 1;
	}

	private static async Task ReturnFromStasis(CardModel card, Player player, PlayerChoiceContext ctx)
	{
		if (card.Keywords.Contains(GuardianKeyword.Volatile))
		{
			await CardCmd.Exhaust(ctx, card, false, false);
			return;
		}
		await CardPileCmd.Add(card, PileTypeExtensions.GetPile((PileType)2, player), (CardPilePosition)1, (AbstractModel)null, false);
		card.EnergyCost.SetUntilPlayed(0, false);
	}

	private static async Task<bool> TickCard(CardModel card, Player player, PlayerChoiceContext ctx)
	{
		if (GuardianCombatModel.StasisCounter[card] <= 0)
		{
			return false;
		}
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return false;
		}
		SpireField<CardModel, int> stasisCounter = GuardianCombatModel.StasisCounter;
		int num = stasisCounter[card];
		stasisCounter[card] = num - 1;
		GuardianDisplay.RefreshCounters(player);
		if (card is ITickCard tickCard)
		{
			await tickCard.OnTick(ctx);
		}
		await GuardianHook.AfterCardTick(combatState, ctx, card, player);
		if (GuardianCombatModel.StasisCounter[card] != 0)
		{
			return false;
		}
		await ReturnFromStasis(card, player, ctx);
		return true;
	}

	public static async Task TickAll(Player player, PlayerChoiceContext ctx)
	{
		List<CardModel> list = player.GetStasis().ToList();
		foreach (CardModel item in list)
		{
			await TickCard(item, player, ctx);
		}
		GuardianDisplay.Refresh(player);
	}

	public static List<GemModel> GetAllCombatGems(Player player)
	{
		return player.GetAllCards().SelectMany(delegate(CardModel card)
		{
			if (card is IGemCard gemCard)
			{
				return new _003C_003Ez__ReadOnlySingleElementList<GemModel>(gemCard.GemModel);
			}
			return (card is IGemSocketCard gemSocketCard) ? gemSocketCard.Gems : Array.Empty<GemModel>();
		}).ToList();
	}

	public static async Task PutGemIn(CardModel gem, CardModel card)
	{
		if (!(card is IGemSocketCard gemSocketCard) || !(gem is IGemCard gemCard))
		{
			return;
		}
		GemModel gemModel = gemCard.GemModel;
		((AbstractModel)card).AssertMutable();
		if (!gemSocketCard.CanAddGem(gemModel))
		{
			return;
		}
		gemSocketCard.AddGem(gemModel);
		await CardPileCmd.RemoveFromDeck(gem, false);
		await Cmd.Wait(0.5f, false);
		if (LocalContext.IsMe(card.Owner))
		{
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi.CardPreviewContainer, (Node)(object)NCardSmithVfx.Create((IEnumerable<CardModel>)new _003C_003Ez__ReadOnlySingleElementList<CardModel>(card), true));
			}
		}
		await Cmd.Wait(0.5f, false);
	}

	public static async Task Brace(PlayerChoiceContext ctx, Player player, decimal amount)
	{
		ModeShiftPower power = player.Creature.GetPower<ModeShiftPower>();
		if (power == null)
		{
			await PowerCmd.Apply<ModeShiftPower>(ctx, player.Creature, 20m, player.Creature, (CardModel)null, true);
			power = player.Creature.GetPower<ModeShiftPower>();
		}
		decimal num = GuardianHook.ModifyBraceAmount(((PowerModel)power).CombatState, player, amount);
		((PowerModel)power).SetAmount((int)((decimal)((PowerModel)power).Amount - num), true);
		while (((PowerModel)power).Amount <= 0)
		{
			await power.Reset(ctx);
		}
	}

	public static Task Brace(PlayerChoiceContext ctx, CardModel card)
	{
		return Brace(ctx, card.Owner, ((DynamicVar)card.DynamicVars.Brace()).IntValue);
	}

	public static async Task AccelerateUntilExit(PlayerChoiceContext ctx, Player player)
	{
		List<CardModel> list = player.GetStasis().ToList();
		foreach (CardModel card in list)
		{
			while (GuardianCombatModel.StasisCounter[card] > 0)
			{
				if (await TickCard(card, player, ctx))
				{
					GuardianDisplay.Refresh(player);
					return;
				}
			}
		}
		GuardianDisplay.Refresh(player);
	}

	public static async Task Accelerate(PlayerChoiceContext ctx, Player player, int amount = 1, AccelerateType accelerateType = AccelerateType.First)
	{
		List<CardModel> list = player.GetStasis().ToList();
		foreach (CardModel card in list)
		{
			int ticks = ((accelerateType == AccelerateType.First) ? Math.Min(amount, GuardianCombatModel.StasisCounter[card]) : amount);
			for (int i = 0; i < ticks; i++)
			{
				await TickCard(card, player, ctx);
			}
			if (accelerateType == AccelerateType.First)
			{
				amount -= ticks;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		GuardianDisplay.Refresh(player);
	}

	public static async Task Accelerate(PlayerChoiceContext ctx, CardModel card, Player player, int amount = 1)
	{
		int ticks = Math.Min(amount, GuardianCombatModel.StasisCounter[card]);
		for (int i = 0; i < ticks; i++)
		{
			await TickCard(card, player, ctx);
		}
		GuardianDisplay.Refresh(player);
	}

	public static Task Accelerate(PlayerChoiceContext ctx, AbstractModel source, AccelerateType accelerateType = AccelerateType.First)
	{
		Player player = source.GetCreature().Player;
		if (player != null)
		{
			return Accelerate(ctx, player, ((DynamicVar)source.GetDynamicVars().Accelerate()).IntValue, accelerateType);
		}
		return Task.CompletedTask;
	}

	public static async Task Polish(PlayerChoiceContext ctx, AbstractModel source)
	{
		int intValue = ((DynamicVar)source.GetDynamicVars().Polish()).IntValue;
		await Polish(ctx, source, intValue);
	}

	public static async Task Polish(PlayerChoiceContext ctx, AbstractModel source, decimal amount)
	{
		await Polish(ctx, source.GetCreature(), amount, (CardModel?)(object)((source is CardModel) ? source : null));
	}

	public static async Task Polish(PlayerChoiceContext ctx, Creature target, decimal amount, CardModel? cardSource)
	{
		await DecrementPower<WeakPower>(ctx, target, amount, cardSource);
		await DecrementPower<FrailPower>(ctx, target, amount, cardSource);
		await DecrementPower<VulnerablePower>(ctx, target, amount, cardSource);
		List<PowerModel> list = target.Powers.Where((PowerModel e) => e is ITemporaryPower).ToList();
		foreach (PowerModel power in list)
		{
			ITemporaryPower val = (ITemporaryPower)power;
			PowerModel power2 = target.GetPower(((AbstractModel)val.InternallyAppliedPower).Id);
			if ((int)val.InternallyAppliedPower.Type != 1 || (int)power.Type != 1)
			{
				continue;
			}
			decimal mod = Math.Min(power.Amount, amount);
			if (!(mod <= 0m))
			{
				if (power2 == null)
				{
					await PowerCmd.Apply(ctx, val.InternallyAppliedPower.ToMutable(0), target, mod, target, cardSource, true);
				}
				power.SetAmount((int)((decimal)power.Amount - mod), false);
				if (power.ShouldRemoveDueToAmount())
				{
					await PowerCmd.Remove(power);
				}
			}
		}
	}

	private static async Task DecrementPower<T>(PlayerChoiceContext ctx, Creature ownerCreature, decimal amount = 1m, CardModel? source = null) where T : PowerModel
	{
		T power = ownerCreature.GetPower<T>();
		if (power != null && ((PowerModel)power).Amount > 0)
		{
			decimal num = Math.Min(((PowerModel)power).Amount, amount);
			await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)power, -num, ownerCreature, source, false);
		}
	}
}
