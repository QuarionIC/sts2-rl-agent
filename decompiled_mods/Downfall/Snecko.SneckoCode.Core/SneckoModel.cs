using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Core;

public class SneckoModel : CustomSingletonModel
{
	public static SavedSpireField<Player, List<ModelId>> SneckoPools = new SavedSpireField<Player, List<ModelId>>((Func<List<ModelId>>)(() => new List<ModelId>()), "SneckoPools")
	{
		Serializer = delegate(List<ModelId> list, PacketWriter writer)
		{
			PacketWriterExtensions.WriteFullModelIdList(writer, (IReadOnlyCollection<ModelId>)list);
		},
		Deserializer = (PacketReader reader) => PacketReaderExtensions.ReadFullModelIdList(reader)
	};

	public SneckoModel()
		: base((HookType)2)
	{
	}

	private static void SetSneckoPools(Player player, IEnumerable<CardPoolModel> pools)
	{
		List<ModelId> list = ((SpireField<Player, List<ModelId>>)(object)SneckoPools).Get(player);
		if (list != null)
		{
			list.Clear();
			list.AddRange(pools.Select((CardPoolModel e) => ((AbstractModel)e).Id));
		}
	}

	private static IEnumerable<CardPoolModel> GetSneckoPools(Player player)
	{
		return ((SpireField<Player, List<ModelId>>)(object)SneckoPools).Get(player)?.Select((Func<ModelId, CardPoolModel>)ModelDb.GetById<CardPoolModel>) ?? Array.Empty<CardPoolModel>();
	}

	public static IEnumerable<CardModel> GetSneckoCards(Player player)
	{
		IEnumerable<CardModel> enumerable = GetSneckoPools(player).SelectMany((CardPoolModel e) => CardFactory.FilterForPlayerCount(player.RunState, e.AllCards));
		if (enumerable != null && enumerable.Any())
		{
			return enumerable;
		}
		return (from c in ModelDb.AllCharacters.Where((CharacterModel e) => e != player.Character).ToList()
			select c.CardPool).ToList().SelectMany((CardPoolModel e) => e.AllCards);
	}

	public static IEnumerable<CardModel> GetRewardSneckoCards(Player player, Func<CardModel, bool>? filter = null)
	{
		IEnumerable<CardModel> enumerable = GetSneckoCards(player);
		if (filter != null)
		{
			enumerable = enumerable.Where(filter);
		}
		return CardFactory.FilterForPlayerCount(player.RunState, CardFactory.FilterForCombat(enumerable));
	}

	public static IEnumerable<CardModel> GetCombatSneckoCards(Player player, int amount, Player? forPlayer = null, Func<CardModel, bool>? filter = null)
	{
		if (forPlayer == null)
		{
			forPlayer = player;
		}
		IEnumerable<CardModel> enumerable = GetSneckoCards(player);
		if (filter != null)
		{
			enumerable = enumerable.Where(filter);
		}
		return CardFactory.GetDistinctForCombat(forPlayer, enumerable, amount, player.RunState.Rng.CombatCardGeneration);
	}

	public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		CardPile pile = card.Pile;
		if (pile != null && (int)pile.Type == 6 && card is IHasGift { Gift: { } gift })
		{
			await SneckoCmd.GetGift(card.Owner, gift);
		}
	}

	public override async Task AfterActEntered()
	{
		RunState state = RunManager.Instance.State;
		if (state == null || state.CurrentActIndex != 0)
		{
			return;
		}
		List<Player> source = state.Players.Where((Player e) => e.Character is Snecko).ToList();
		Dictionary<Player, uint[]> choiceIds = source.ToDictionary((Player snecko) => snecko, (Player snecko) => (from _ in Enumerable.Range(0, 3)
			select RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(snecko)).ToArray());
		(Player, List<CardPoolModel>)[] array = await Task.WhenAll(source.Select(async (Player snecko) => (snecko: snecko, pools: await SneckoPoolSelection.DoOffclassSelection(snecko, (IRunState)(object)state, choiceIds[snecko]))));
		for (int num = 0; num < array.Length; num++)
		{
			var (player, pools) = array[num];
			SetSneckoPools(player, pools);
		}
	}
}
