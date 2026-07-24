using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Features;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions;

public static class AbstractModelExtensions
{
	public static Creature GetCreature(this AbstractModel model)
	{
		RelicModel val = (RelicModel)(object)((model is RelicModel) ? model : null);
		if (val == null)
		{
			CardModel val2 = (CardModel)(object)((model is CardModel) ? model : null);
			if (val2 == null)
			{
				PotionModel val3 = (PotionModel)(object)((model is PotionModel) ? model : null);
				if (val3 == null)
				{
					PowerModel val4 = (PowerModel)(object)((model is PowerModel) ? model : null);
					if (val4 == null)
					{
						EnchantmentModel val5 = (EnchantmentModel)(object)((model is EnchantmentModel) ? model : null);
						if (val5 == null)
						{
							AfflictionModel val6 = (AfflictionModel)(object)((model is AfflictionModel) ? model : null);
							if (val6 == null)
							{
								CardModifier val7 = (CardModifier)(object)((model is CardModifier) ? model : null);
								if (val7 != null)
								{
									return ((AbstractModel)(object)val7.Owner)?.GetCreature() ?? throw new ArgumentException("Unknown model type: " + ((object)model).GetType().Name);
								}
								throw new ArgumentException("Unknown model type: " + ((object)model).GetType().Name);
							}
							return ((AbstractModel)(object)val6.Card).GetCreature();
						}
						return ((AbstractModel)(object)val5.Card).GetCreature();
					}
					return val4.Owner;
				}
				return val3.Owner.Creature;
			}
			return val2.Owner.Creature;
		}
		return val.Owner.Creature;
	}

	public static DynamicVarSet GetDynamicVars(this AbstractModel model)
	{
		RelicModel val = (RelicModel)(object)((model is RelicModel) ? model : null);
		if (val == null)
		{
			CardModel val2 = (CardModel)(object)((model is CardModel) ? model : null);
			if (val2 == null)
			{
				PotionModel val3 = (PotionModel)(object)((model is PotionModel) ? model : null);
				if (val3 == null)
				{
					PowerModel val4 = (PowerModel)(object)((model is PowerModel) ? model : null);
					if (val4 == null)
					{
						EnchantmentModel val5 = (EnchantmentModel)(object)((model is EnchantmentModel) ? model : null);
						if (val5 == null)
						{
							AfflictionModel val6 = (AfflictionModel)(object)((model is AfflictionModel) ? model : null);
							if (val6 == null)
							{
								CardModifier val7 = (CardModifier)(object)((model is CardModifier) ? model : null);
								if (val7 != null)
								{
									return val7.DynamicVars;
								}
								throw new ArgumentException("Unknown model type: " + ((object)model).GetType().Name);
							}
							return val6.Card.DynamicVars;
						}
						return val5.DynamicVars;
					}
					return val4.DynamicVars;
				}
				return val3.DynamicVars;
			}
			return val2.DynamicVars;
		}
		return val.DynamicVars;
	}

	public static TargetType GetTargetType(this AbstractModel model)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val == null)
		{
			PotionModel val2 = (PotionModel)(object)((model is PotionModel) ? model : null);
			if (val2 == null)
			{
				CardModifier val3 = (CardModifier)(object)((model is CardModifier) ? model : null);
				if (val3 != null)
				{
					CardModel owner = val3.Owner;
					return (TargetType)((owner != null) ? ((int)owner.TargetType) : 0);
				}
				throw new ArgumentException("Unknown model type: " + ((object)model).GetType().Name);
			}
			return val2.TargetType;
		}
		return val.TargetType;
	}

	public static IEnumerable<Creature> MyGetTargets(this AbstractModel model, Creature? singleTarget = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val == null)
		{
			return model.ResolveTargets(model.GetTargetType(), singleTarget);
		}
		TargetType targetType = val.TargetType;
		bool flag = (((int)targetType == 2 || targetType - 5 <= 1) ? true : false);
		if (flag || CustomTargetType.IsCustomSingleTargetType(targetType))
		{
			if (singleTarget == null)
			{
				return Array.Empty<Creature>();
			}
			return new _003C_003Ez__ReadOnlySingleElementList<Creature>(singleTarget);
		}
		return CardExtensions.GetTargets(val);
	}

	public static IEnumerable<Creature> MyGetTargets(this AbstractModel model, Creature? singleTarget, TargetType targetTypeOverride)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return model.ResolveTargets(targetTypeOverride, singleTarget);
	}

	private static IEnumerable<Creature> ResolveTargets(this AbstractModel model, TargetType type, Creature? singleTarget)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		Creature creature = model.GetCreature();
		ICombatState combatState = creature.CombatState;
		switch (type - 1)
		{
		case 0:
			return new _003C_003Ez__ReadOnlySingleElementList<Creature>(creature);
		case 1:
		case 4:
		case 5:
		{
			IEnumerable<Creature> result2;
			if (singleTarget == null)
			{
				IEnumerable<Creature> enumerable = Array.Empty<Creature>();
				result2 = enumerable;
			}
			else
			{
				IEnumerable<Creature> enumerable = new _003C_003Ez__ReadOnlySingleElementList<Creature>(singleTarget);
				result2 = enumerable;
			}
			return result2;
		}
		case 2:
			if (combatState != null)
			{
				return combatState.HittableEnemies;
			}
			break;
		case 6:
			if (combatState != null)
			{
				return combatState.PlayerCreatures.Where((Creature c) => c != null && c.IsAlive);
			}
			break;
		case 3:
			if (combatState != null)
			{
				Creature val = combatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)combatState.HittableEnemies);
				IEnumerable<Creature> result;
				if (val == null)
				{
					IEnumerable<Creature> enumerable = Array.Empty<Creature>();
					result = enumerable;
				}
				else
				{
					IEnumerable<Creature> enumerable = new _003C_003Ez__ReadOnlySingleElementList<Creature>(val);
					result = enumerable;
				}
				return result;
			}
			break;
		}
		throw new InvalidOperationException($"Unsupported TargetType {type} for {((object)model).GetType().Name}");
	}
}
