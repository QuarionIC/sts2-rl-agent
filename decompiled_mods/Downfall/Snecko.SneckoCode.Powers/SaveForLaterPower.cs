using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class SaveForLaterPower : SneckoPowerModel
{
	public SaveForLaterPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip((CardKeyword)5);
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(((PowerModel)this).Owner))
		{
			return;
		}
		Player player = ((PowerModel)this).Owner.Player;
		if (((player != null) ? player.Creature.CombatState : null) == null || player != ((PowerModel)this).Owner.Player || !Hook.ShouldFlush(player.Creature.CombatState, player))
		{
			return;
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.RetainSelectionPrompt, 0, ((PowerModel)this).Amount);
		List<CardModel> list = (await CardSelectCmd.FromHand(choiceContext, ((PowerModel)this).Owner.Player, val, (Func<CardModel, bool>)RetainFilter, (AbstractModel)(object)this)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (CardModel item in list)
		{
			item.GiveSingleTurnRetain();
		}
		await PowerCmd.Remove((PowerModel)(object)this);
	}

	private static bool RetainFilter(CardModel card)
	{
		return !card.ShouldRetainThisTurn;
	}
}
