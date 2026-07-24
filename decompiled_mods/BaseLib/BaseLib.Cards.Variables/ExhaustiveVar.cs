using System;
using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class ExhaustiveVar : DynamicVar
{
	public const string Key = "Exhaustive";

	public ExhaustiveVar(decimal exhaustiveCount)
		: base("Exhaustive", exhaustiveCount)
	{
		this.WithTooltip();
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		((DynamicVar)this).PreviewValue = ExhaustiveCount(card, ((DynamicVar)this).IntValue);
	}

	public static int ExhaustiveCount(CardModel card, int baseExhaustive)
	{
		if (baseExhaustive <= 0)
		{
			return 0;
		}
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => entry.CardPlay.Card == card);
		return Math.Max(1, baseExhaustive - num);
	}
}
