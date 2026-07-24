using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Patches.Features;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Utils;

public static class CommonActions
{
	[Obsolete("Use an overload that receives a CardPlay parameter. This is required on the beta branch.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			return CardAttack(card, target, card.DynamicVars.CalculatedDamage, card.DynamicVars.CalculatedDamage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		if (card.DynamicVars.ContainsKey("Damage"))
		{
			return CardAttack(card, target, ((DynamicVar)card.DynamicVars.Damage).BaseValue, card.DynamicVars.Damage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		throw new Exception("Card " + card.Title + " does not have a damage variable supported by CommonActions.CardAttack");
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? play, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			return CardAttack(card, play, (play != null) ? play.Target : null, card.DynamicVars.CalculatedDamage, card.DynamicVars.CalculatedDamage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		if (card.DynamicVars.ContainsKey("Damage"))
		{
			return CardAttack(card, play, (play != null) ? play.Target : null, ((DynamicVar)card.DynamicVars.Damage).BaseValue, card.DynamicVars.Damage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		throw new Exception("Card " + card.Title + " does not have a damage variable supported by CommonActions.CardAttack");
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, decimal damage, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, target, damage, (ValueProp)8, hitCount, vfx, sfx, tmpSfx);
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, decimal damage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		return CardAttack(card, null, target, damage, valueProp, hitCount, vfx, sfx, tmpSfx);
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? cardPlay, Creature? target, decimal damage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected I4, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		AttackCommand val = DamageCmd.Attack(damage).WithHitCount(hitCount).WithValueProp(valueProp)
			.FromCardCompatibility(card, cardPlay);
		if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
		{
			if (target == null)
			{
				return val;
			}
			val.Targeting(target);
		}
		else if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
		{
			ICombatState combatState = card.CombatState;
			if (combatState == null)
			{
				return val;
			}
			IEnumerable<Creature> targets = combatState.Creatures.Where((Creature c) => CustomTargetType.CanMultiTarget(card.TargetType, c, card.Owner));
			val.TargetingFiltered(targets);
		}
		else
		{
			TargetType targetType = card.TargetType;
			switch (targetType - 2)
			{
			case 0:
				if (target == null)
				{
					return val;
				}
				val.Targeting(target);
				break;
			case 1:
			{
				ICombatState combatState3 = card.CombatState;
				if (combatState3 == null)
				{
					return val;
				}
				val.TargetingAllOpponents(combatState3);
				break;
			}
			case 2:
			{
				ICombatState combatState2 = card.CombatState;
				if (combatState2 == null)
				{
					return val;
				}
				val.TargetingRandomOpponents(combatState2, true);
				break;
			}
			default:
				throw new Exception($"Unsupported AttackCommand target type {card.TargetType} for card {card.Title}");
			}
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			val.WithHitFx(vfx, sfx, tmpSfx);
		}
		return val;
	}

	public static AttackCommand CardAttack(CardModel card, Creature? target, CalculatedDamageVar calculatedDamage, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, target, calculatedDamage, (ValueProp)8, hitCount, vfx, sfx, tmpSfx);
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, CalculatedDamageVar calculatedDamage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		return CardAttack(card, null, target, calculatedDamage, valueProp, hitCount, vfx, sfx, tmpSfx);
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? cardPlay, Creature? target, CalculatedDamageVar calculatedDamage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected I4, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		AttackCommand val = DamageCmd.Attack(calculatedDamage).WithHitCount(hitCount).WithValueProp(valueProp)
			.FromCardCompatibility(card, cardPlay);
		if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
		{
			if (target == null)
			{
				return val;
			}
			val.Targeting(target);
		}
		else if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
		{
			ICombatState combatState = card.CombatState;
			if (combatState == null)
			{
				return val;
			}
			IEnumerable<Creature> targets = combatState.Creatures.Where((Creature c) => CustomTargetType.CanMultiTarget(card.TargetType, c, card.Owner));
			val.TargetingFiltered(targets);
		}
		else
		{
			TargetType targetType = card.TargetType;
			switch (targetType - 2)
			{
			case 0:
				if (target == null)
				{
					return val;
				}
				val.Targeting(target);
				break;
			case 1:
			{
				ICombatState combatState3 = card.CombatState;
				if (combatState3 == null)
				{
					return val;
				}
				val.TargetingAllOpponents(combatState3);
				break;
			}
			case 2:
			{
				ICombatState combatState2 = card.CombatState;
				if (combatState2 == null)
				{
					return val;
				}
				val.TargetingRandomOpponents(combatState2, true);
				break;
			}
			default:
				throw new Exception($"Unsupported AttackCommand target type {card.TargetType} for card {card.Title}");
			}
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			val.WithHitFx(vfx, sfx, tmpSfx);
		}
		return val;
	}

	public static async Task<decimal> CardBlock(CardModel card, CardPlay? play)
	{
		DynamicVar var = default(DynamicVar);
		if (card.DynamicVars.TryGetValue("Block", ref var))
		{
			return await CardBlock(card, var, play);
		}
		if (card.DynamicVars.TryGetValue("CalculatedBlock", ref var))
		{
			return await CardBlock(card, var, play);
		}
		throw new InvalidOperationException($"No valid block var found in card {((object)card).GetType()} for CommonActions.CardBlock; define a block var or " + "pass a variable in manually.");
	}

	public static async Task<decimal> CardBlock(CardModel card, BlockVar blockVar, CardPlay? play)
	{
		return await CreatureCmd.GainBlock(card.Owner.Creature, blockVar, play, false);
	}

	public static async Task<decimal> CardBlock(CardModel card, DynamicVar var, CardPlay? play, bool fast = false)
	{
		CalculatedBlockVar val = (CalculatedBlockVar)(object)((var is CalculatedBlockVar) ? var : null);
		if (val != null)
		{
			return await CreatureCmd.GainBlock(card.Owner.Creature, ((CalculatedVar)val).Calculate(card.Owner.Creature), val.Props, play, fast);
		}
		Creature creature = card.Owner.Creature;
		decimal baseValue = var.BaseValue;
		DynamicVar obj = ((var is BlockVar) ? var : null);
		return await CreatureCmd.GainBlock(creature, baseValue, (ValueProp)((obj == null) ? 8 : ((int)((BlockVar)obj).Props)), play, fast);
	}

	public static async Task<IEnumerable<CardModel>> Draw(CardModel card, PlayerChoiceContext context)
	{
		return await CardPileCmd.Draw(context, ((DynamicVar)card.DynamicVars.Cards).BaseValue, card.Owner, false);
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			(object)new ThrowingPlayerChoiceContext(),
			target,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<IReadOnlyList<T>> Apply<T>(IEnumerable<Creature> targets, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<IReadOnlyList<T>>, T>(null, new object[6]
		{
			(object)new ThrowingPlayerChoiceContext(),
			targets,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, CardModel card, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			(object)new ThrowingPlayerChoiceContext(),
			target,
			card.DynamicVars.Power<T>().BaseValue,
			card.Owner.Creature,
			card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, CardModel? card, decimal amount, bool silent = false) where T : PowerModel
	{
		VariableMethod apply = BetaMainCompatibility.PowerCmd_.Apply;
		object[] obj = new object[6]
		{
			(object)new ThrowingPlayerChoiceContext(),
			target,
			amount,
			null,
			null,
			null
		};
		obj[3] = ((card != null) ? card.Owner.Creature : null);
		obj[4] = card;
		obj[5] = silent;
		return await apply.InvokeGeneric<Task<T>, T>(null, obj);
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> ApplySelf<T>(CardModel card, bool silent = false) where T : PowerModel
	{
		return await ApplySelf<T>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), card, card.DynamicVars.Power<T>().BaseValue, silent);
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> ApplySelf<T>(CardModel card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			(object)new ThrowingPlayerChoiceContext(),
			card.Owner.Creature,
			amount,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			target,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext context, IEnumerable<Creature> targets, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<IReadOnlyList<T>>, T>(null, new object[6]
		{
			context,
			targets,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, CardModel card, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			target,
			card.DynamicVars.Power<T>().BaseValue,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, CardModel? card, decimal amount, bool silent = false) where T : PowerModel
	{
		VariableMethod apply = BetaMainCompatibility.PowerCmd_.Apply;
		object[] obj = new object[6] { context, target, amount, null, null, null };
		obj[3] = ((card != null) ? card.Owner.Creature : null);
		obj[4] = card;
		obj[5] = silent;
		return await apply.InvokeGeneric<Task<T>, T>(null, obj);
	}

	public static async Task<T?> ApplySelf<T>(PlayerChoiceContext context, CardModel card, bool silent = false) where T : PowerModel
	{
		return await ApplySelf<T>(context, card, card.DynamicVars.Power<T>().BaseValue, silent);
	}

	public static async Task<T?> ApplySelf<T>(PlayerChoiceContext context, CardModel card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			card.Owner.Creature,
			amount,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, CardSelectorPrefs prefs, PlayerChoiceContext context, PileType pileType)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return await SelectCards(card, prefs, context, pileType, null);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, CardSelectorPrefs prefs, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		CardPile pile = PileTypeExtensions.GetPile(pileType, card.Owner);
		IReadOnlyList<CardModel> readOnlyList = pile.Cards;
		if ((int)pile.Type == 1)
		{
			readOnlyList = (from c in readOnlyList
				orderby c.Rarity, ((AbstractModel)c).Id
				select c).ToList();
		}
		if ((int)pile.Type == 2)
		{
			return await CardSelectCmd.FromHand(context, card.Owner, prefs, filter, (AbstractModel)(object)card);
		}
		if (filter != null)
		{
			readOnlyList = readOnlyList.Where(filter).ToList();
		}
		return await CardSelectCmd.FromSimpleGrid(context, readOnlyList, card.Owner, prefs);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, int count = 1)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return await SelectCards(card, selectionPrompt, context, pileType, null, count);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter, int count = 1)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		CardSelectorPrefs prefs = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref prefs))._002Ector(selectionPrompt, count);
		return await SelectCards(card, prefs, context, pileType, filter);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, int minCount, int maxCount)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return await SelectCards(card, selectionPrompt, context, pileType, null, minCount, maxCount);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter, int minCount, int maxCount)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		CardSelectorPrefs prefs = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref prefs))._002Ector(selectionPrompt, minCount, maxCount);
		return await SelectCards(card, prefs, context, pileType, filter);
	}

	public static async Task<CardModel?> SelectSingleCard(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return await SelectSingleCard(card, selectionPrompt, context, pileType, null);
	}

	public static async Task<CardModel?> SelectSingleCard(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		CardSelectorPrefs prefs = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref prefs))._002Ector(selectionPrompt, 1);
		return (await SelectCards(card, prefs, context, pileType, filter)).FirstOrDefault();
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext ctx, CardModel card, CardPlay? cardPlay, bool silent = false) where T : PowerModel
	{
		if (((cardPlay != null) ? cardPlay.Target : null) != null)
		{
			T val = await Apply<T>(ctx, cardPlay.Target, card, silent);
			IReadOnlyList<T> result;
			if (val == null)
			{
				IReadOnlyList<T> readOnlyList = Array.Empty<T>();
				result = readOnlyList;
			}
			else
			{
				IReadOnlyList<T> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<T>(val);
				result = readOnlyList;
			}
			return result;
		}
		return await ApplyToCreatures<T>(card, ctx, card.GetTargets(), silent);
	}

	private static async Task<IReadOnlyList<T>> ApplyToCreatures<T>(CardModel card, PlayerChoiceContext ctx, params Creature[] targets) where T : PowerModel
	{
		return await Apply<T>(ctx, (IEnumerable<Creature>)targets, (DynamicVarSource)card, silent: false);
	}

	private static async Task<IReadOnlyList<T>> ApplyToCreatures<T>(CardModel card, PlayerChoiceContext ctx, IEnumerable<Creature> targets, bool silent = false) where T : PowerModel
	{
		return await Apply<T>(ctx, targets, (DynamicVarSource)card, silent);
	}

	public static IEnumerable<CardModel> GenerateCards(CardModel card, int count, Func<CardModel, bool>? filter = null)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Player owner = card.Owner;
		IEnumerable<CardModel> enumerable = owner.Character.CardPool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint);
		if (filter != null)
		{
			enumerable = enumerable.Where(filter).ToList();
		}
		return CardFactory.GetDistinctForCombat(owner, enumerable, count, owner.RunState.Rng.CombatCardGeneration);
	}

	public static CardModel? GenerateSingleCard(CardModel card, Func<CardModel, bool>? filter = null)
	{
		return GenerateCards(card, 1, filter).FirstOrDefault();
	}
}
