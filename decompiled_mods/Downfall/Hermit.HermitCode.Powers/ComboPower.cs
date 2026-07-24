using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class ComboPower : HermitPowerModel, IModifyCardPlayResultLocation
{
	private class Data
	{
		public int DeadOnCardsPlayed;
	}

	public override int DisplayAmount => Math.Max(0, ((PowerModel)this).Amount - ((PowerModel)this).GetInternalData<Data>().DeadOnCardsPlayed);

	public CardLocationCompatiblity ModifyCardPlayResultLocationCompability(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocationCompatiblity cardLocation)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		if (((PowerModel)this).GetInternalData<Data>().DeadOnCardsPlayed >= ((PowerModel)this).Amount || card.Owner.Creature != ((PowerModel)this).Owner || !HermitCmd.IsDeadOn(card) || (int)card.Type == 3 || card.Keywords.Contains((CardKeyword)1))
		{
			return cardLocation;
		}
		((PowerModel)this).Flash();
		SetDeadOnCardsPlayed(((PowerModel)this).GetInternalData<Data>().DeadOnCardsPlayed + 1);
		return new CardLocationCompatiblity(card.Owner, (PileType)2, (CardPilePosition)1);
	}

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return Task.CompletedTask;
		}
		SetDeadOnCardsPlayed(0);
		return Task.CompletedTask;
	}

	private void SetDeadOnCardsPlayed(int value)
	{
		((PowerModel)this).GetInternalData<Data>().DeadOnCardsPlayed = value;
		((PowerModel)this).InvokeDisplayAmountChanged();
	}

	public ComboPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
