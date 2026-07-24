using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Core;

public static class SlimeBossCmd
{
	private static IEnumerable<SlimeModel> GetSlimes(Player player)
	{
		return player.Creature.Pets.Select((Creature e) => e.Monster).OfType<SlimeModel>();
	}

	private static SlimeModel? GetFirstSlime(Player player)
	{
		return GetSlimes(player).LastOrDefault();
	}

	public static Task<bool> Absorb(PlayerChoiceContext ctx, CardModel card)
	{
		return Absorb(ctx, card.Owner, card);
	}

	public static async Task<bool> Absorb(PlayerChoiceContext ctx, Player player, CardModel? card = null)
	{
		bool a = await SlimeQueue.RemoveLeadingSlime(player);
		await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, 1m, player.Creature, card, false);
		return a;
	}

	public static async Task<int> AbsorbAll(PlayerChoiceContext ctx, Player player, CardModel? card = null)
	{
		int a = await SlimeQueue.RemoveAll(player);
		await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, (decimal)a, player.Creature, card, false);
		return a;
	}

	public static Task<int> AbsorbAll(PlayerChoiceContext ctx, CardModel card)
	{
		return AbsorbAll(ctx, card.Owner, card);
	}

	private static async Task CommandInternal(PlayerChoiceContext ctx, Player player, CommandType commandType = CommandType.First)
	{
		switch (commandType)
		{
		case CommandType.First:
		{
			SlimeModel firstSlime = GetFirstSlime(player);
			if (firstSlime != null)
			{
				await firstSlime.Command(ctx);
			}
			break;
		}
		case CommandType.All:
			await GetSlimes(player).Reverse().ForEachAsync((SlimeModel s) => s.Command(ctx));
			break;
		default:
			throw new ArgumentOutOfRangeException("commandType", commandType, null);
		}
	}

	public static async Task Command(PlayerChoiceContext ctx, Player player, int amount, ValueProp props, CardModel? cardSource = null, CommandType commandType = CommandType.First)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		int modified = amount;
		if (!((Enum)props).HasFlag((Enum)(object)(ValueProp)4))
		{
			ICombatState combatState = player.Creature.CombatState;
			if (combatState == null)
			{
				return;
			}
			modified = SlimeBossHook.ModifyConsumeCount(combatState, player, amount, cardSource, out IEnumerable<IModifyConsumeCount> modifiers);
			await SlimeBossHook.AfterModifyingConsumeCount(combatState, modifiers, player, cardSource);
		}
		for (int i = 0; i < modified; i++)
		{
			await CommandInternal(ctx, player, commandType);
		}
	}

	public static Task Command(PlayerChoiceContext ctx, CardModel card, ValueProp props = (ValueProp)8)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return Command(ctx, card.Owner, card.DynamicVars["Command"].IntValue, props, card);
	}

	public static Task CommandAll(PlayerChoiceContext ctx, Player player, CardModel card, ValueProp props)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return Command(ctx, player, card.DynamicVars["Command"].IntValue, props, card, CommandType.All);
	}

	public static Task CommandAll(PlayerChoiceContext ctx, Player player, ValueProp props, int amount = 1, CardModel? cardSource = null)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return Command(ctx, player, amount, props, cardSource, CommandType.All);
	}

	public static async Task SlurpAll(CardModel card)
	{
		await CardPileCmd.Add((IEnumerable<CardModel>)(from e in card.Owner.GetExhaust()
			where e.Tags.Contains(SlimeBossTag.Lick)
			select e).ToList(), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}

	public static async Task Slurp(Player player, int amount)
	{
		List<CardModel> source = (from e in player.GetExhaust()
			where e.Tags.Contains(SlimeBossTag.Lick)
			select e).ToList();
		List<CardModel> list = source.Where((CardModel e) => !e.Keywords.Contains(SlimeBossKeyword.Buried)).ToList();
		List<CardModel> list2 = source.Where((CardModel e) => e.Keywords.Contains(SlimeBossKeyword.Buried)).ToList();
		List<CardModel> list3 = IEnumerableExtensions.TakeRandom<CardModel>((IEnumerable<CardModel>)list, Math.Min(amount, list.Count), player.RunState.Rng.CombatCardSelection).ToList();
		if (list3.Count < amount)
		{
			list3.AddRange(IEnumerableExtensions.TakeRandom<CardModel>((IEnumerable<CardModel>)list2, amount - list3.Count, player.RunState.Rng.CombatCardSelection));
		}
		await CardPileCmd.Add((IEnumerable<CardModel>)list3, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}

	public static async Task<int> DecreaseSlots(PlayerChoiceContext ctx, Player player, int amount = 1)
	{
		var (actual, num) = await SlimeQueue.DecreaseSlimeSlots(player, amount);
		if (num > 0)
		{
			await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, (decimal)num, player.Creature, (CardModel)null, false);
		}
		return actual;
	}

	public static Task IncreaseSlots(Player player, int amount = 1)
	{
		return SlimeQueue.IncreaseSlimeSlots(player, amount);
	}

	public static Task Slurp(CardModel card)
	{
		return Slurp(card.Owner, card.DynamicVars["Slurp"].IntValue);
	}

	public static Task Split<T>(PlayerChoiceContext ctx, Player player) where T : SlimeModel
	{
		return Split(ctx, player, SlimeBossModelDb.Slime<T>());
	}

	private static async Task Split(PlayerChoiceContext ctx, Player player, SlimeModel slimeModel)
	{
		var (flag, num) = await SlimeQueue.AddSlime(player, slimeModel);
		if (flag)
		{
			if (num > 0)
			{
				await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, (decimal)num, player.Creature, (CardModel)null, false);
			}
			if (player.Creature.CombatState != null)
			{
				await SlimeBossHook.AfterSplit(player.Creature.CombatState, player, slimeModel);
			}
		}
	}

	public static async Task SplitRandom(PlayerChoiceContext ctx, Player player, SlimeType slimeType)
	{
		if (player.Creature.CombatState != null)
		{
			SlimeModel slimeModel = player.RunState.Rng.CombatCardGeneration.NextItem<SlimeModel>(SlimeBossModelDb.AllSlimes.Where((SlimeModel e) => (e.SlimeType & slimeType) != 0));
			if (slimeModel != null)
			{
				await Split(ctx, player, slimeModel);
			}
		}
	}

	public static async Task SplitSpecialist(PlayerChoiceContext ctx, Player player)
	{
		ICombatState combatState = player.Creature.CombatState;
		if (combatState != null)
		{
			List<CardModel> list = (from e in IEnumerableExtensions.TakeRandom<SlimeModel>(SlimeBossModelDb.AllSpecialistSlimes, 3, player.RunState.Rng.CombatCardGeneration).Select(SlimeBossModelDb.GetCardForSlime)
				select combatState.CreateCard(e, player)).ToList();
			if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, player, false) is ISlimeCard slimeCard)
			{
				SlimeModel slimeModel = slimeCard.SlimeModel;
				await Split(ctx, player, slimeModel);
			}
		}
	}
}
