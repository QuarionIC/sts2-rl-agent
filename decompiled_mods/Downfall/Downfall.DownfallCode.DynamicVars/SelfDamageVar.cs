using System.Collections.Generic;
using Downfall.DownfallCode.Events;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.DynamicVars;

public class SelfDamageVar : DamageVar
{
	public SelfDamageVar(decimal damage, ValueProp props)
		: base("SelfDamage", damage, props)
	{
	}//IL_0007: Unknown result type (might be due to invalid IL or missing references)


	public SelfDamageVar(string name, decimal damage, ValueProp props)
		: base(name, damage, props)
	{
	}//IL_0003: Unknown result type (might be due to invalid IL or missing references)


	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		decimal num = ((DynamicVar)this).BaseValue;
		if (runGlobalHooks && card.CombatState != null)
		{
			num = DownfallHook.ModifySelfDamage(card.CombatState, num, (AbstractModel)(object)card, out IEnumerable<IModifySelfDamage> _);
		}
		((DynamicVar)this).PreviewValue = num;
	}
}
