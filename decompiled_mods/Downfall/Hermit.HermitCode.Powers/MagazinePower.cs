using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public class MagazinePower : HermitPowerModel, IModifyDamageAdditive
{
	public MagazinePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip((CardKeyword)5);
	}

	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (ValuePropExtensions.IsPoweredAttack(props) && cardSource != null && IsBasicStrike(cardSource) && dealer == ((PowerModel)this).Owner && !CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && IsBasicStrike(e.CardPlay.Card) && e.CardPlay.Card.Owner.Creature == ((PowerModel)this).Owner))
		{
			return ((PowerModel)this).Amount;
		}
		return 0m;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (!card.IsBasicStrikeOrDefend || card.Owner != ((PowerModel)this).Owner.Player)
		{
			return Task.CompletedTask;
		}
		CardCmd.ApplyKeyword(card, (CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		return Task.CompletedTask;
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		Player player = ((PowerModel)this).Owner.Player;
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			obj = ((playerCombatState != null) ? playerCombatState.AllCards.Where((CardModel c) => c.IsBasicStrikeOrDefend) : null);
		}
		IEnumerable<CardModel> enumerable = (IEnumerable<CardModel>)obj;
		if (enumerable == null)
		{
			return Task.CompletedTask;
		}
		foreach (CardModel item in enumerable)
		{
			CardCmd.ApplyKeyword(item, (CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		}
		return Task.CompletedTask;
	}

	private static bool IsBasicStrike(CardModel card)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)card.Rarity == 1)
		{
			return card.Tags.Contains((CardTag)1);
		}
		return false;
	}
}
