using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.CustomEnums;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class CoalescencePower : HermitPowerModel
{
	private static bool RetainFilter(CardModel card)
	{
		return !card.ShouldRetainThisTurn;
	}

	public override async Task BeforeFlushLate(PlayerChoiceContext ctx, Player player)
	{
		if (player != ((PowerModel)this).Owner.Player || player.Creature.CombatState == null || !Hook.ShouldFlush(player.Creature.CombatState, player))
		{
			return;
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.RetainSelectionPrompt, 0, ((PowerModel)this).Amount);
		List<CardModel> list = (await CardSelectCmd.FromHand(ctx, ((PowerModel)this).Owner.Player, val, (Func<CardModel, bool>)RetainFilter, (AbstractModel)(object)this)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (CardModel item in list)
		{
			item.GiveSingleTurnRetain();
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public CoalescencePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
