using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Uncommon;
using Awakened.AwakenedCode.Displays;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Piles;
using Awakened.AwakenedCode.Powers;
using Awakened.AwakenedCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace Awakened.AwakenedCode.Core;

public static class AwakenedCmd
{
	public static AwakenedPile GetSpellbookOrThrow(Player player)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return (AwakenedPile)(object)PileTypeExtensions.GetPile(AwakenedPile.Spellbook, player);
	}

	public static bool WasLastCardPlayedPower(CardModel card)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		if (!CombatManager.Instance.IsInProgress)
		{
			return false;
		}
		CardPlayStartedEntry val = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((Func<CardPlayStartedEntry, bool>)((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card != card));
		if (val == null)
		{
			return false;
		}
		return (int)val.CardPlay.Card.Type == 3;
	}

	public static bool WasLastCardPlayedPower(CardPlay cardPlay)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		if (!CombatManager.Instance.IsInProgress)
		{
			return false;
		}
		CardPlayStartedEntry val = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((Func<CardPlayStartedEntry, bool>)((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == cardPlay.Card.Owner && e.CardPlay != cardPlay));
		if (val == null)
		{
			return false;
		}
		return (int)val.CardPlay.Card.Type == 3;
	}

	public static async Task Awaken(Player player, PlayerChoiceContext ctx)
	{
		if (!AwakenedModel.MarkAwakened(player))
		{
			return;
		}
		Callable val = Callable.From((Action)delegate
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature obj = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
			if (((obj != null) ? obj.Visuals : null) is NAwakenedCreatureVisuals nAwakenedCreatureVisuals)
			{
				nAwakenedCreatureVisuals.IsAwakened = true;
				nAwakenedCreatureVisuals.OnAnimationTrigger("Idle");
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		await AwakenedHook.OnAwaken(player.Creature.CombatState, ctx, player);
	}

	public static async Task Chant(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		if (card is IChantable chantable)
		{
			bool firstTime = !chantable.HasChanted;
			if (firstTime && !(card is Caw))
			{
				TalkCmd.Play(new LocString("monsters", "DAMP_CULTIST.moves.INCANTATION.banter"), card.Owner.Creature, (VfxColor)2, (VfxDuration)6);
				SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_damp", 1f);
			}
			chantable.HasChanted = true;
			await chantable.PlayChantEffect(ctx, cardPlay);
			await AwakenedHook.OnCardChanted(card.CombatState, ctx, card, cardPlay, firstTime);
		}
	}

	private static bool CanConjure(Player player)
	{
		return !player.Creature.Powers.Any((PowerModel p) => p is BurnoutPower);
	}

	public static async Task<CardModel?> Conjure(Player player)
	{
		if (!CanConjure(player))
		{
			return null;
		}
		AwakenedPile orInitSpellbook = AwakenedModel.GetOrInitSpellbook(player);
		Rng combatCardSelection = player.RunState.Rng.CombatCardSelection;
		CardModel val = orInitSpellbook.NextSpell ?? ((((CardPile)orInitSpellbook).Cards.Count > 0) ? ((CardPile)orInitSpellbook).Cards[0] : null);
		if (val == null)
		{
			return null;
		}
		return await ConjureSpell(player, val, orInitSpellbook, combatCardSelection);
	}

	public static async Task<CardModel?> ConjureSelected(Player player, CardModel sourceCard, CardModel selectedSpell)
	{
		if (!CanConjure(player))
		{
			return null;
		}
		AwakenedPile orInitSpellbook = AwakenedModel.GetOrInitSpellbook(player);
		Rng combatCardSelection = sourceCard.CombatState.RunState.Rng.CombatCardSelection;
		if (!((CardPile)orInitSpellbook).Cards.Contains(selectedSpell))
		{
			return null;
		}
		return await ConjureSpell(player, selectedSpell, orInitSpellbook, combatCardSelection);
	}

	private static async Task<CardModel?> ConjureSpell(Player player, CardModel spell, AwakenedPile spellbook, Rng rng)
	{
		((CardPile)spellbook).RemoveInternal(spell, false);
		spellbook.SetNextSpell(rng);
		await CardPileCmd.Add(spell, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		if (((CardPile)spellbook).Cards.Count == 0)
		{
			spellbook.Refresh(player);
		}
		AwakenedDisplay.Refresh(player);
		return spell;
	}
}
