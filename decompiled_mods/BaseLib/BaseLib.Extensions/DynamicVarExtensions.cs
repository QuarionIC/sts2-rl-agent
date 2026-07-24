using System;
using System.Collections.Generic;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Extensions;

public static class DynamicVarExtensions
{
	[HarmonyPatch(typeof(DynamicVar), "Clone")]
	private class CloneTooltips
	{
		[HarmonyPostfix]
		private static DynamicVar Copy(DynamicVar __result, DynamicVar __instance)
		{
			DynamicVarTips[__result] = DynamicVarTips[__instance];
			DynamicVarUpgrades[__result] = DynamicVarUpgrades[__instance];
			return __result;
		}
	}

	public static readonly SpireField<DynamicVar, Func<DynamicVar, IHoverTip>> DynamicVarTips = new SpireField<DynamicVar, Func<DynamicVar, IHoverTip>>(() => (Func<DynamicVar, IHoverTip>?)null);

	public static readonly SpireField<DynamicVar, decimal?> DynamicVarUpgrades = new SpireField<DynamicVar, decimal?>(() => (decimal?)null);

	public static TDynamicVar WithUpgrade<TDynamicVar>(this TDynamicVar dynamicVar, decimal upgradeValue) where TDynamicVar : DynamicVar
	{
		if (upgradeValue != 0m)
		{
			DynamicVarUpgrades[(DynamicVar)(object)dynamicVar] = upgradeValue;
		}
		return dynamicVar;
	}

	public static decimal CalculateBlock(this DynamicVar var, Creature creature, ValueProp props, CardPlay? cardPlay = null, CardModel? cardSource = null)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		decimal baseValue = var.BaseValue;
		if (!CombatManager.Instance.IsInProgress)
		{
			return baseValue;
		}
		if (CombatManager.Instance.IsEnding)
		{
			return baseValue;
		}
		ICombatState combatState = creature.CombatState;
		if (combatState == null)
		{
			return baseValue;
		}
		EnchantmentModel val = ((cardSource != null) ? cardSource.Enchantment : null);
		if (val != null)
		{
			baseValue += val.EnchantBlockAdditive(baseValue);
			baseValue *= val.EnchantBlockMultiplicative(baseValue);
		}
		IEnumerable<AbstractModel> enumerable = default(IEnumerable<AbstractModel>);
		baseValue = Hook.ModifyBlock(combatState, creature, baseValue, props, cardSource, cardPlay, ref enumerable);
		return Math.Max(baseValue, 0m);
	}

	public static TDynamicVar WithTooltip<TDynamicVar>(this TDynamicVar var, string? locKey = null, string locTable = "static_hover_tips") where TDynamicVar : DynamicVar
	{
		string key = locKey ?? (((object)var).GetType().GetPrefix() + StringHelper.Slugify(((DynamicVar)var).Name));
		DynamicVarTips[(DynamicVar)(object)var] = delegate(DynamicVar locVar)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected O, but got Unknown
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Expected O, but got Unknown
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			LocString val = new LocString(locTable, key + ".title");
			string text = (LocString.Exists(locTable, key + ".smartDescription") ? (key + ".smartDescription") : (key + ".description"));
			LocString val2 = new LocString(locTable, text);
			val.Add(locVar);
			val2.Add(locVar);
			return (IHoverTip)(object)new HoverTip(val, val2, (Texture2D)null);
		};
		return var;
	}
}
