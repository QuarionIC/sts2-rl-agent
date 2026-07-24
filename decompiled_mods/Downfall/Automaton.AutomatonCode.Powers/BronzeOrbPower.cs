using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Piles;
using Automaton.AutomatonCode.Vfx;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class BronzeOrbPower : AutomatonPowerModel, IModifyCardPlayResultLocation
{
	public CardLocationCompatiblity ModifyCardPlayResultLocationCompability(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocationCompatiblity cardLocation)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (card.Owner.Creature != ((PowerModel)this).Owner || card.Keywords.Contains((CardKeyword)1) || AutomatonCmd.IsEncodable(card) || (int)card.Type == 3)
		{
			return cardLocation;
		}
		NStashDisplay.EnsureFor(card.Owner);
		return new CardLocationCompatiblity(card.Owner, StashPile.Stash, (CardPilePosition)2);
	}

	public Task AfterModifyingCardPlayResultLocationCompability(CardModel card, CardLocationCompatiblity cardLocation)
	{
		return PowerCmd.Decrement((PowerModel)(object)this);
	}

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return Task.CompletedTask;
		}
		PowerCmd.Remove((PowerModel)(object)this);
		return Task.CompletedTask;
	}

	public BronzeOrbPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
