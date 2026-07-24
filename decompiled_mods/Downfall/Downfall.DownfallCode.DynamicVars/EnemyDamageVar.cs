using System.Collections.Generic;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.DynamicVars;

public class EnemyDamageVar : DynamicVar
{
	public ValueProp Props { get; }

	public EnemyDamageVar(decimal damage, ValueProp props)
		: base("EnemyDamage", damage)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Props = props;
	}

	public EnemyDamageVar(string name, decimal damage, ValueProp props)
		: base(name, damage)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Props = props;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		decimal previewValue = ((DynamicVar)this).BaseValue;
		if (runGlobalHooks)
		{
			previewValue = CompatibilityHook.ModifyDamage(card.Owner.RunState, card.CombatState, card.Owner.Creature, target, ((DynamicVar)this).BaseValue, Props, card, null, (ModifyDamageHookType)14, previewMode, out IEnumerable<AbstractModel> _);
		}
		((DynamicVar)this).PreviewValue = previewValue;
	}
}
