using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Downfall.DownfallCode.DynamicVars;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Downfall.DownfallCode.Commands;

public class DownfallCmd
{
	public static Task GainTempHp(PlayerChoiceContext ctx, CardModel card)
	{
		return GainTempHp(ctx, card, card.DynamicVars["TempHP"].BaseValue);
	}

	public static Task GainTempHp(PlayerChoiceContext ctx, CardModel card, decimal tempHp)
	{
		return PowerCmd.Apply<TempHpPower>(ctx, card.Owner.Creature, tempHp, card.Owner.Creature, card, false);
	}

	public static Task GainTempHp(PlayerChoiceContext ctx, Creature creature, decimal tempHp)
	{
		return PowerCmd.Apply<TempHpPower>(ctx, creature, tempHp, creature, (CardModel)null, false);
	}

	public static int GetTempHpAmount(Creature creature)
	{
		return creature.GetPowerAmount<TempHpPower>();
	}

	public static async Task EnemyAttackPlayer(PlayerChoiceContext ctx, CardPlay cardPlay, CardModel card)
	{
		Creature target = cardPlay.Target;
		MonsterModel val = ((target != null) ? target.Monster : null);
		if (cardPlay.Target != null && val != null && cardPlay.Target.IsAlive)
		{
			Player player = card.Owner;
			Creature attacker = val.Creature;
			await Cmd.Wait(0.5f, false);
			EnemyDamageVar enemyDamageVar = card.DynamicVars.EnemyDamage();
			AttackCommand obj = DamageCmd.Attack(((DynamicVar)enemyDamageVar).BaseValue);
			obj.Attacker = attacker;
			obj._attackerAnimName = "Attack";
			obj._sourceType = (SourceType)2;
			await AttackCommandExtensions.WithValueProp(obj.Targeting(player.Creature), enemyDamageVar.Props).WithHitFx("vfx/vfx_attack_slash", "event:/sfx/characters/silent/silent_attack", (string)null).Execute(ctx);
		}
	}

	public static async Task Steal<T>(PlayerChoiceContext ctx, CardPlay cardPlay, CardModel card) where T : PowerModel
	{
		IEnumerable<Creature> targets = ((AbstractModel)(object)card).MyGetTargets(cardPlay.Target);
		await Steal<T>(ctx, targets, card);
	}

	public static Task Steal<T>(PlayerChoiceContext ctx, Creature target, CardModel card) where T : PowerModel
	{
		return Steal<T>(ctx, (IEnumerable<Creature>)new _003C_003Ez__ReadOnlySingleElementList<Creature>(target), card);
	}

	private static async Task Steal<T>(PlayerChoiceContext ctx, IEnumerable<Creature> targets, CardModel card) where T : PowerModel
	{
		decimal a = DynamicVarSetExtensions.Power<T>(card.DynamicVars).BaseValue;
		Creature player = card.Owner.Creature;
		await PowerCmd.Apply<T>(ctx, targets, -a, player, card, false);
		await PowerCmd.Apply<T>(ctx, player, a, player, card, false);
	}

	public static Creature? GainPet<T>(Player summoner) where T : MonsterModel
	{
		ICombatState combatState = summoner.Creature.CombatState;
		if (combatState == null)
		{
			return null;
		}
		return ((IEnumerable<Creature>)combatState.Allies).FirstOrDefault((Func<Creature, bool>)((Creature c) => c.Monster is T && c.PetOwner == summoner));
	}

	public static async Task<Creature> Summon<T>(PlayerChoiceContext ctx, Player summoner, int hp, AbstractModel? source) where T : MonsterModel
	{
		ICombatState combatState = summoner.Creature.CombatState;
		Creature existing = ((combatState != null) ? ((IEnumerable<Creature>)combatState.Allies).FirstOrDefault((Func<Creature, bool>)((Creature c) => c.Monster is T && c.PetOwner == summoner)) : null);
		bool isReviving = existing != null && !existing.IsAlive;
		if (existing != null && existing.IsAlive)
		{
			await CreatureCmd.GainMaxHp(existing, (decimal)hp);
			return existing;
		}
		if (isReviving && existing != null)
		{
			PlayerCombatState playerCombatState = summoner.PlayerCombatState;
			if (playerCombatState != null)
			{
				playerCombatState.AddPetInternal(existing);
			}
		}
		else
		{
			existing = await PlayerCmd.AddPet<T>(summoner);
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature node = ((instance != null) ? instance.GetCreatureNode(existing) : null);
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature val = ((instance2 != null) ? instance2.GetCreatureNode(summoner.Creature) : null);
			if (node != null && source is CardModel && val != null)
			{
				((Control)node).Position = ((Control)val).Position + new Vector2(250f, -75f);
				((CanvasItem)node).Modulate = Colors.Transparent;
				((Node)node).CreateTween().TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("modulate"), Variant.op_Implicit(Colors.White), 0.35).SetDelay(0.1);
			}
			await PowerCmd.Apply<DieForYouPower>(ctx, existing, 1m, (Creature)null, (CardModel)null, false);
			if (node != null)
			{
				node.TrackBlockStatus(summoner.Creature);
			}
			if (node != null)
			{
				node.ToggleIsInteractable(true);
			}
		}
		await CreatureCmd.SetMaxHp(existing, (decimal)hp);
		await CreatureCmd.Heal(existing, (decimal)hp, isReviving);
		return existing;
	}

	public static bool IsDebuffed(Creature? creature)
	{
		if (creature == null)
		{
			return false;
		}
		return creature.Powers.Any((PowerModel e) => (int)e.TypeForCurrentAmount == 2);
	}
}
