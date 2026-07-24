using System;
using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class PersistVar : DynamicVar
{
	public const string Key = "Persist";

	public PersistVar(decimal persistCount)
		: base("Persist", persistCount)
	{
		this.WithTooltip();
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		((DynamicVar)this).PreviewValue = PersistCount(card, ((DynamicVar)this).IntValue);
	}

	public static int PersistCount(CardModel card, int basePersist)
	{
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(card.CombatState) && entry.CardPlay.Card == card);
		return Math.Max(0, basePersist - num);
	}
}
