using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Hexaghost.HexaghostCode.Core;

public static class HexaghostCmd
{
	public static GhostflameModel[] GetWheel(Player player)
	{
		return HexaghostModel.Wheel[player] ?? Array.Empty<GhostflameModel>();
	}

	public static int GetCurrentIndex(Player player)
	{
		return HexaghostModel.CurrentIndex[player];
	}

	public static GhostflameModel GetCurrentFlame(Player player)
	{
		return GetWheel(player)[GetCurrentIndex(player)];
	}

	public static T? GetFlameOfType<T>(Player player) where T : GhostflameModel
	{
		return GetWheel(player).OfType<T>().FirstOrDefault();
	}

	public static int GetIgnitedCount(Player player)
	{
		return GetWheel(player).Count((GhostflameModel f) => f.IsIgnited);
	}

	public static bool AllIgnited(Player player)
	{
		return GetWheel(player).All((GhostflameModel f) => f.IsIgnited);
	}

	private static int GetPreviousIndex(Player player)
	{
		GhostflameModel[] wheel = GetWheel(player);
		return (GetCurrentIndex(player) + wheel.Length - 1) % wheel.Length;
	}

	private static int GetNextIndex(Player player)
	{
		GhostflameModel[] wheel = GetWheel(player);
		return (GetCurrentIndex(player) + 1) % wheel.Length;
	}

	public static async Task Advance(PlayerChoiceContext ctx, Player player, AbstractModel? source, bool silent = false, bool autoAdvance = false)
	{
		await MoveTo(player, GetNextIndex(player));
		if (!autoAdvance)
		{
			await HexaghostHook.AfterWheelAdvance(player.Creature.CombatState, ctx, player, source, GetCurrentFlame(player), GetCurrentIndex(player), silent);
		}
	}

	public static async Task Retract(PlayerChoiceContext ctx, Player player, AbstractModel? source, bool silent = false)
	{
		await MoveTo(player, GetPreviousIndex(player));
		await HexaghostHook.AfterWheelRetract(player.Creature.CombatState, ctx, player, source, GetCurrentFlame(player), GetCurrentIndex(player), silent);
	}

	public static async Task MoveToRandom(PlayerChoiceContext ctx, Player player, bool silent = false)
	{
		GhostflameModel[] wheel = GetWheel(player);
		int current = GetCurrentIndex(player);
		Rng niche = player.RunState.Rng.Niche;
		int[] array = (from i in Enumerable.Range(0, wheel.Length)
			where i != current
			select i).ToArray();
		int index = niche.NextItem<int>((IEnumerable<int>)array);
		await MoveTo(player, index, silent);
	}

	public static Task ReplaceCurrentWithRandom(Player player)
	{
		GhostflameModel[] wheel = GetWheel(player);
		int currentIndex = GetCurrentIndex(player);
		Rng niche = player.RunState.Rng.Niche;
		Type currentType = ((object)wheel[currentIndex]).GetType();
		GhostflameModel[] array = HexaghostModelDb.AllGhostflames.Where((GhostflameModel f) => ((object)f).GetType() != currentType).ToArray();
		GhostflameModel ghostflameModel = niche.NextItem<GhostflameModel>((IEnumerable<GhostflameModel>)array);
		if (ghostflameModel == null)
		{
			return Task.CompletedTask;
		}
		wheel[currentIndex] = ghostflameModel.ToMutable(player);
		HexaghostVisualsBridge.Refresh(player);
		return Task.CompletedTask;
	}

	private static Task MoveTo(Player player, int index, bool silent = false)
	{
		HexaghostModel.CurrentIndex[player] = index;
		GhostflameModel currentFlame = GetCurrentFlame(player);
		currentFlame.Extinguish();
		currentFlame.UpdateVisuals();
		if (silent)
		{
			return Task.CompletedTask;
		}
		HexaghostVisualsBridge.Refresh(player);
		return Task.CompletedTask;
	}

	public static bool IsIgnited(Player player)
	{
		return GetCurrentFlame(player).IsIgnited;
	}

	public static bool IsPreviousIgnited(Player player)
	{
		return GetWheel(player)[GetPreviousIndex(player)].IsIgnited;
	}

	public static bool IsNextIgnited(Player player)
	{
		return GetWheel(player)[GetNextIndex(player)].IsIgnited;
	}

	public static Task IgnitePrevious(PlayerChoiceContext ctx, Player player)
	{
		return IgniteAt(ctx, player, GetPreviousIndex(player));
	}

	public static Task IgniteNext(PlayerChoiceContext ctx, Player player)
	{
		return IgniteAt(ctx, player, GetNextIndex(player));
	}

	public static Task Ignite(PlayerChoiceContext ctx, Player player)
	{
		return IgniteAt(ctx, player, GetCurrentIndex(player));
	}

	public static async Task IgniteAt(PlayerChoiceContext ctx, Player player, int index)
	{
		await Cmd.Wait(0.05f, false);
		GhostflameModel flame = GetWheel(player)[index];
		if (!flame.IsIgnited)
		{
			flame.IsIgnited = true;
		}
		bool allIgnited = AllIgnited(player);
		flame.SetIgniteProgress();
		HexaghostVisualsBridge.Refresh(player);
		await flame.OnIgnite(ctx);
		await HexaghostHook.AfterGhostwheelIgnited(player.Creature.CombatState, ctx, player, flame, index);
		await Cmd.Wait(0.05f, false);
		if (allIgnited)
		{
			await HexaghostHook.AfterGhostwheelAllIgnited(player.Creature.CombatState, ctx, player, flame, index);
		}
	}

	public static async Task IgniteAll(PlayerChoiceContext ctx, Player player)
	{
		GhostflameModel[] wheel = GetWheel(player);
		for (int i = 0; i < wheel.Length; i++)
		{
			await IgniteAt(ctx, player, i);
		}
	}

	public static Task ExtinguishAllExceptThis(PlayerChoiceContext ctx, Player player, GhostflameModel model)
	{
		foreach (GhostflameModel item in from e in GetWheel(player)
			where e != model
			select e)
		{
			item.Extinguish();
		}
		HexaghostVisualsBridge.Refresh(player);
		return Task.CompletedTask;
	}

	public static Task Extinguish(Player player, bool silent = false)
	{
		GetCurrentFlame(player).Extinguish();
		if (silent)
		{
			return Task.CompletedTask;
		}
		HexaghostVisualsBridge.Refresh(player);
		return Task.CompletedTask;
	}

	public static Task<int> ResetWheel(Player player)
	{
		Cmd.Wait(0.1f, false);
		int result = GetWheel(player).Count((GhostflameModel flame) => flame.Extinguish());
		Cmd.Wait(0.1f, false);
		HexaghostModel.ResetWheel(player);
		Cmd.Wait(0.1f, false);
		HexaghostVisualsBridge.Refresh(player);
		return Task.FromResult(result);
	}

	public static void SetCurrentGhostflame(Player player, GhostflameModel ghostflame)
	{
		((AbstractModel)ghostflame).AssertCanonical();
		GetWheel(player)[GetCurrentIndex(player)] = ghostflame.ToMutable(player);
		HexaghostVisualsBridge.Refresh(player);
	}
}
