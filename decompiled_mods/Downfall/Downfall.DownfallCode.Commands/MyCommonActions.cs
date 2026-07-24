using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Patches.Features;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.DynamicVars;
using Downfall.DownfallCode.Events;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Commands;

public static class MyCommonActions
{
	public static Task<T?> ApplySelf<T>(PlayerChoiceContext ctx, AbstractModel model) where T : PowerModel
	{
		Creature creature = model.GetCreature();
		DynamicVarSet dynamicVars = model.GetDynamicVars();
		return PowerCmd.Apply<T>(ctx, creature, DynamicVarSetExtensions.Power<T>(dynamicVars).BaseValue, creature, (CardModel)(object)((model is CardModel) ? model : null), false);
	}

	public static Task Block(AbstractModel model, CardPlay? play = null)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		DynamicVarSet dynamicVars = model.GetDynamicVars();
		Creature creature = model.GetCreature();
		DynamicVar val = default(DynamicVar);
		if (dynamicVars.TryGetValue("CalculatedBlock", ref val))
		{
			CalculatedBlockVar val2 = (CalculatedBlockVar)(object)((val is CalculatedBlockVar) ? val : null);
			if (val2 != null)
			{
				return CreatureCmd.GainBlock(creature, ((CalculatedVar)val2).Calculate((play != null) ? play.Target : null), val2.Props, play, false);
			}
		}
		DynamicVar val3 = default(DynamicVar);
		if (dynamicVars.TryGetValue("Block", ref val3))
		{
			BlockVar val4 = (BlockVar)(object)((val3 is BlockVar) ? val3 : null);
			if (val4 != null)
			{
				return CreatureCmd.GainBlock(creature, val4, play, false);
			}
		}
		throw new InvalidOperationException(((object)model).GetType().Name + " does not have a Block or CalculatedBlock var");
	}

	public static async Task<IEnumerable<DamageResult>> SelfDamage(PlayerChoiceContext ctx, AbstractModel model)
	{
		ICombatState combatState = model.GetCreature().CombatState;
		if (combatState == null)
		{
			return Array.Empty<DamageResult>();
		}
		SelfDamageVar damage = model.GetDynamicVars().SelfDamage();
		IEnumerable<IModifySelfDamage> modifiers;
		decimal modified = DownfallHook.ModifySelfDamage(combatState, ((DynamicVar)damage).BaseValue, model, out modifiers);
		await DownfallHook.AfterModifyingSelfDamage(combatState, modifiers, model);
		if (modified <= 0m)
		{
			return Array.Empty<DamageResult>();
		}
		return await CreatureCmd.Damage(ctx, model.GetCreature(), modified, ((DamageVar)damage).Props, model.GetCreature());
	}

	public static async Task LoseHpToTarget(PlayerChoiceContext ctx, AbstractModel model, Creature target)
	{
		await DownfallCreatureCmd.Damage(ctx, target, ((DynamicVar)model.GetDynamicVars().HpLoss).BaseValue, (ValueProp)6, model.GetCreature(), (CardModel?)(object)((model is CardModel) ? model : null), null);
	}

	public static async Task LoseHpToTarget(PlayerChoiceContext ctx, AbstractModel model, IEnumerable<Creature> targets)
	{
		await DownfallCreatureCmd.Damage(ctx, targets, ((DynamicVar)model.GetDynamicVars().HpLoss).BaseValue, (ValueProp)6, model.GetCreature(), (CardModel?)(object)((model is CardModel) ? model : null), null);
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext ctx, AbstractModel model, Creature? target = null) where T : PowerModel
	{
		Creature creature = model.GetCreature();
		decimal baseValue = DynamicVarSetExtensions.Power<T>(model.GetDynamicVars()).BaseValue;
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		List<Creature> list = model.MyGetTargets(target).ToList();
		if (list.Count != 1)
		{
			return await PowerCmd.Apply<T>(ctx, (IEnumerable<Creature>)list, baseValue, creature, val, false);
		}
		T val2 = await PowerCmd.Apply<T>(ctx, list[0], baseValue, creature, val, false);
		IReadOnlyList<T> result;
		if (val2 == null)
		{
			IReadOnlyList<T> readOnlyList = Array.Empty<T>();
			result = readOnlyList;
		}
		else
		{
			IReadOnlyList<T> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<T>(val2);
			result = readOnlyList;
		}
		return result;
	}

	public static async Task LoseHp(PlayerChoiceContext ctx, AbstractModel model, Creature? target = null)
	{
		await LoseHpToTarget(ctx, model, model.MyGetTargets(target));
	}

	public static AttackCommand Attack(AbstractModel model, Creature? target = null, TargetType? targetTypeOverride = null, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		DynamicVarSet dynamicVars = model.GetDynamicVars();
		AttackCommand val;
		if (dynamicVars.ContainsKey("CalculatedDamage"))
		{
			val = AttackCommandExtensions.WithValueProp(DamageCmd.Attack(dynamicVars.CalculatedDamage), dynamicVars.CalculatedDamage.Props);
		}
		else
		{
			if (!dynamicVars.ContainsKey("Damage"))
			{
				throw new InvalidOperationException(((object)model).GetType().Name + " does not have a Damage or CalculatedDamage var");
			}
			val = AttackCommandExtensions.WithValueProp(DamageCmd.Attack(((DynamicVar)dynamicVars.Damage).BaseValue), dynamicVars.Damage.Props);
		}
		val.WithHitCount(hitCount);
		val.FromModel(model);
		List<Creature> list = ((!targetTypeOverride.HasValue) ? model.MyGetTargets(target).ToList() : model.MyGetTargets(target, targetTypeOverride.Value).ToList());
		int count = list.Count;
		if (count <= 1)
		{
			switch (count)
			{
			case 0:
			{
				ICombatState combatState = model.GetCreature().CombatState;
				if (combatState == null)
				{
					throw new InvalidOperationException(((object)model).GetType().Name + " requested an AllEnemies attack with no combat state.");
				}
				val.TargetingAllOpponents(combatState);
				break;
			}
			case 1:
				val.Targeting(list[0]);
				break;
			}
		}
		else
		{
			AttackCommandExtensions.TargetingFiltered(val, (IEnumerable<Creature>)list);
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			val.WithHitFx(vfx, sfx, tmpSfx);
		}
		return val;
	}

	private static AttackCommand FromModel(this AttackCommand cmd, AbstractModel model)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val != null)
		{
			return BetaMainCompatibility.FromCardCompatibility(cmd, val, (CardPlay)null);
		}
		if (cmd.Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		cmd.Attacker = model.GetCreature();
		cmd.ModelSource = model;
		cmd._attackerAnimName = "Attack";
		cmd._sourceType = (SourceType)1;
		return cmd;
	}

	public static async Task<IEnumerable<CardModel>> Draw(AbstractModel card, PlayerChoiceContext context)
	{
		Player player = card.GetCreature().Player;
		if (player == null)
		{
			return Array.Empty<CardModel>();
		}
		return await CardPileCmd.Draw(context, ((DynamicVar)card.GetDynamicVars().Cards).BaseValue, player, false);
	}
}
