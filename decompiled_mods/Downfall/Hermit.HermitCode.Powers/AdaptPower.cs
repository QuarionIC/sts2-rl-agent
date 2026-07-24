using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class AdaptPower : HermitPowerModel
{
	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)side != 1 || ((PowerModel)this).Owner.Player == null || CombatManager.Instance.IsOverOrEnding || !PileTypeExtensions.GetPile((PileType)2, ((PowerModel)this).Owner.Player).Cards.Any())
		{
			return;
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.ExhaustSelectionPrompt, 0, ((PowerModel)this).Amount);
		foreach (CardModel item in await CardSelectCmd.FromHand(ctx, ((PowerModel)this).Owner.Player, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this))
		{
			await CardCmd.Exhaust(ctx, item, false, false);
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, 8m, (ValueProp)4, (CardPlay)null, false);
		}
	}

	public AdaptPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
