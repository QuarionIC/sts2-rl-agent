using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Events;
using Automaton.AutomatonCode.Interfaces;
using Automaton.AutomatonCode.Piles;
using Automaton.AutomatonCode.Relics;
using Automaton.AutomatonCode.Vfx;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Automaton.AutomatonCode.Core;

public static class AutomatonCmd
{
	public static int GetMax(Player creature)
	{
		if (creature.GetRelic<ElectromagneticCoil>() != null)
		{
			return 4;
		}
		return 3;
	}

	public static async Task<FunctionCard?> EncodeCard(CardModel card, PlayerChoiceContext ctx)
	{
		Player creature = card.Owner;
		CustomPile pile = CustomPiles.GetCustomPile(creature.PlayerCombatState, EncodePile.FunctionSequence);
		if (pile == null)
		{
			return null;
		}
		if (LocalContext.IsMe(creature))
		{
			CardPile pile2 = card.Pile;
			if (pile2 != null && (int)pile2.Type == 2)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NPlayerHand obj = ((instance != null) ? instance.Ui.Hand : null);
				if (obj != null)
				{
					obj.Remove(card);
				}
			}
		}
		await Cmd.Wait(0.2f, false);
		await CardPileCmd.Add(card, (CardPile)(object)pile, (CardPilePosition)1, (AbstractModel)null, false);
		await Cmd.Wait(0.2f, false);
		NSequenceDisplay.Refresh(creature);
		FunctionCard functionCard = null;
		if (((CardPile)pile).Cards.Count >= GetMax(creature))
		{
			functionCard = await CompileFunctionCard(creature, ctx);
		}
		await AutomatonHook.OnCardEncoded(creature.Creature.CombatState, ctx, card);
		return functionCard;
	}

	private static async Task<FunctionCard?> CompileFunctionCard(Player player, PlayerChoiceContext ctx)
	{
		CustomPile pile = CustomPiles.GetCustomPile(player.PlayerCombatState, EncodePile.FunctionSequence);
		if (pile == null)
		{
			return null;
		}
		await Cmd.Wait(0.3f, false);
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return null;
		}
		List<CardModel> snapshot = ((CardPile)pile).Cards.ToList();
		((CardPile)pile).Clear(true);
		NSequenceDisplay.Refresh(player);
		foreach (CardModel item in snapshot)
		{
			if (item is ICompilable compilable)
			{
				await compilable.OnCompile(ctx);
			}
		}
		FunctionCard functionCard = combatState.CreateCard<FunctionCard>(player);
		functionCard.SetSourceCards(snapshot);
		functionCard = AutomatonHook.ModifyCompiledFunction(combatState, functionCard, player, out IEnumerable<IModifyCompiledFunction> modifiers);
		await AutomatonHook.AfterModifyCompiledFunction(combatState, modifiers, player, functionCard);
		await AutomatonHook.AfterCompilingFunction(ctx, combatState, player, await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)functionCard, (PileType)2, player, (CardPilePosition)1));
		return functionCard;
	}

	public static bool IsEncodable(CardModel card)
	{
		if (card is IEncodable encodable)
		{
			return encodable.CanPlayerEncode;
		}
		return false;
	}

	public static async Task EncodeEffect(CardModel card, PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (!(card is IEncodable encodable))
		{
			return;
		}
		foreach (Encodable encoding in encodable.Encodings)
		{
			await encoding.OnPlay((AbstractModel)(object)card, ctx, cardPlay.Target, cardPlay);
		}
	}
}
