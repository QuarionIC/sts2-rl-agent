using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class ScryVar : DynamicVar
{
	public ScryVar(decimal baseValue)
		: base("Scry", baseValue)
	{
		this.WithTooltip();
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		int num = ((DynamicVar)this).IntValue;
		if (runGlobalHooks)
		{
			num = BaseLibHooks.ModifyScryAmount(card.Owner, num, out IEnumerable<IModifyScryAmount> _);
		}
		((DynamicVar)this).PreviewValue = num;
	}
}
