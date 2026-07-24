using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.DynamicVars;

public class GemVar : DynamicVar
{
	public GemVar(string name, decimal baseValue)
		: base(name, baseValue)
	{
	}

	public GemVar(decimal baseValue)
		: base("Gem", baseValue)
	{
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		decimal previewValue = ((DynamicVar)this).BaseValue;
		if (runGlobalHooks && card.CombatState != null && base._owner is GemModel gem)
		{
			previewValue = GuardianHook.ModifyGemEffect(card.CombatState, gem, ((DynamicVar)this).BaseValue, card);
		}
		((DynamicVar)this).PreviewValue = previewValue;
	}

	public override string ToString()
	{
		return $"{((DynamicVar)this).PreviewValue}";
	}
}
