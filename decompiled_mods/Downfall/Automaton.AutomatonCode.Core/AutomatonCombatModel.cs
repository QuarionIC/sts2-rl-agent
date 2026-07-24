using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Events;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Core;

public class AutomatonCombatModel : CustomSingletonModel
{
	public AutomatonCombatModel()
		: base((HookType)1)
	{
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		IEnumerable<IModifyStashDraw> modifiers;
		int n = AutomatonHook.ModifyStashDraw(combatState, 1, player, out modifiers);
		foreach (CardPileAddResult item in await StashCmd.DrawFromStash(player, n))
		{
			CardModel cardAdded = item.cardAdded;
			CombatManager.Instance.History.Add(combatState, (CombatHistoryEntry)new CardDrawnEntry(cardAdded, combatState.RoundNumber, combatState.CurrentSide, false, CombatManager.Instance.History, (IEnumerable<Player>)combatState.Players));
			await Hook.AfterCardDrawn(combatState, ctx, cardAdded, false);
		}
	}
}
