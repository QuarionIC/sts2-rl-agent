using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Events;

namespace SlimeBoss.SlimeBossCode.DynamicVars;

public class CommandVar : DynamicVar
{
	public CommandVar(decimal value)
		: base("Command", value)
	{
		DynamicVarExtensions.WithTooltip<CommandVar>(this, (string)null, "static_hover_tips");
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		int num = ((DynamicVar)this).IntValue;
		if (runGlobalHooks && card.CombatState != null)
		{
			num = SlimeBossHook.ModifyConsumeCount(card.CombatState, card.Owner, num, card, out IEnumerable<IModifyConsumeCount> _);
		}
		((DynamicVar)this).PreviewValue = num;
	}
}
