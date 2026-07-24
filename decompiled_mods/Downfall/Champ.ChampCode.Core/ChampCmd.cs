using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Enchantments;
using Champ.ChampCode.Events;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Core;

public class ChampCmd
{
	public static async Task EnterBerserkerStance(PlayerChoiceContext ctx, Player player, bool force = false)
	{
		if (force || !(player.ChampStance() is ChampUltimateStance champUltimateStance))
		{
			await ChampModel.SetStance<ChampBerserkerStance>(ctx, player);
		}
		else
		{
			champUltimateStance.ResetCharges();
		}
	}

	public static async Task EnterDefensiveStance(PlayerChoiceContext ctx, Player player, bool force = false)
	{
		if (force || !(player.ChampStance() is ChampUltimateStance champUltimateStance))
		{
			await ChampModel.SetStance<ChampDefensiveStance>(ctx, player);
		}
		else
		{
			champUltimateStance.ResetCharges();
		}
	}

	public static async Task EnterUltimateStance(PlayerChoiceContext ctx, Player player)
	{
		await ChampModel.SetStance<ChampUltimateStance>(ctx, player);
	}

	public static async Task EnterStance<T>(PlayerChoiceContext ctx, Player player) where T : ChampStanceModel
	{
		await ChampModel.SetStance<T>(ctx, player);
	}

	public static async Task EnterDifferentStance(PlayerChoiceContext ctx, Player owner)
	{
		ChampStanceModel champStanceModel = owner.ChampStance();
		if (!(champStanceModel is ChampBerserkerStance))
		{
			if (!(champStanceModel is ChampDefensiveStance))
			{
				await EnterRandomStance(ctx, owner);
			}
			else
			{
				await EnterBerserkerStance(ctx, owner);
			}
		}
		else
		{
			await EnterDefensiveStance(ctx, owner);
		}
	}

	public static async Task EnterRandomStance(PlayerChoiceContext ctx, Player owner)
	{
		if (owner.Creature.CombatState.RunState.Rng.CombatCardSelection.NextBool())
		{
			await EnterDefensiveStance(ctx, owner);
		}
		else
		{
			await EnterBerserkerStance(ctx, owner);
		}
	}

	public static async Task ClearStance(PlayerChoiceContext ctx, Player player)
	{
		await ChampModel.SetStance<ChampNoStance>(ctx, player);
	}

	public static async Task PlayFinisher(PlayerChoiceContext ctx, CardPlay cardPlay, bool skipClear = false, int repeat = 1)
	{
		Player player = cardPlay.Card.Owner;
		ChampStanceModel m = player.ChampStance();
		if (!m.HasFinisher)
		{
			return;
		}
		for (int i = 0; i < repeat; i++)
		{
			await m.Finisher(ctx);
			await ChampHook.OnFinisher(player.Creature.CombatState, ctx, cardPlay);
		}
		if (!skipClear && !(cardPlay.Card.Enchantment is Signature))
		{
			await ClearStance(ctx, player);
			if (m is ChampUltimateStance)
			{
				await EnterStance<ChampUltimateStance>(ctx, player);
			}
		}
	}

	public static async Task SelectStanceToEnter(PlayerChoiceContext ctx, Player owner)
	{
		List<CardModel> list = new List<CardModel>
		{
			(CardModel)(object)owner.Creature.CombatState.CreateCard<StanceDanceBerserker>(owner),
			(CardModel)(object)owner.Creature.CombatState.CreateCard<StanceDanceDefensive>(owner)
		};
		CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, owner, false);
		if (val == null)
		{
			return;
		}
		if (!(val is StanceDanceBerserker))
		{
			if (val is StanceDanceDefensive)
			{
				await EnterDefensiveStance(ctx, owner);
			}
		}
		else
		{
			await EnterBerserkerStance(ctx, owner);
		}
	}
}
