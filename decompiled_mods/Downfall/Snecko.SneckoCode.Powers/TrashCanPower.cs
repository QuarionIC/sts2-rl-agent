using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class TrashCanPower : SneckoPowerModel
{
	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Owner.Player != null)
		{
			CardSelectorPrefs val = default(CardSelectorPrefs);
			((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.ExhaustSelectionPrompt, 0, ((PowerModel)this).Amount);
			(await CardSelectCmd.FromHand(ctx, ((PowerModel)this).Owner.Player, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).ToList().ForEach(delegate(CardModel e)
			{
				CardCmd.Exhaust(ctx, e, false, false);
			});
		}
	}

	public TrashCanPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
