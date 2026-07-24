using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Afflictions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class EntangledPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)2;

	public override PowerStackType StackType => (PowerStackType)2;

	public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		AFTPModAudio.Play("general", "entangle");
		foreach (CardModel card in ((PowerModel)this).Owner.Player.PlayerCombatState.AllCards.Where((CardModel c) => (int)c.Type == 1))
		{
			await CardCmd.Afflict<EntangledOriginal>(card, 1m);
		}
	}

	public override async Task AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner == ((PowerModel)this).Owner.Player && card.Affliction == null && (int)card.Type == 1)
		{
			await CardCmd.Afflict<EntangledOriginal>(card, 1m);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public override Task AfterRemoved(Creature oldOwner)
	{
		foreach (CardModel item in oldOwner.Player.PlayerCombatState.AllCards.Where((CardModel c) => c.Affliction is EntangledOriginal))
		{
			CardCmd.ClearAffliction(item);
		}
		return Task.CompletedTask;
	}
}
