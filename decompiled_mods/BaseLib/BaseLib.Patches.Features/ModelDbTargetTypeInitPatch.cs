using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(ModelDb), "Init")]
internal static class ModelDbTargetTypeInitPatch
{
	[HarmonyPostfix]
	private static void RegisterTargetTypes()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.Anyone, (Creature target) => target != null && target.IsAlive && !target.IsPet);
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.Everyone, (Creature target) => target != null && target.IsAlive && !target.IsPet);
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.AnyAttackingEnemy, delegate(Creature target)
		{
			if (target != null && target.IsAlive && target.IsEnemy)
			{
				MonsterModel monster = target.Monster;
				if (monster != null)
				{
					return monster.IntendsToAttack;
				}
			}
			return false;
		});
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllAttackingEnemies, delegate(Creature target)
		{
			if (target != null && target.IsAlive && target.IsEnemy)
			{
				MonsterModel monster = target.Monster;
				if (monster != null)
				{
					return monster.IntendsToAttack;
				}
			}
			return false;
		});
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.AnyBlockingEnemy, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.Block > 0);
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllBlockingEnemies, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.Block > 0);
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.AnyNonBlockingEnemy, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.Block == 0);
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllNonBlockingEnemies, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.Block == 0);
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllLowestHpEnemies, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.CurrentHp == target.CombatState.Enemies.Where((Creature e) => e.IsAlive).Min((Creature e) => e.CurrentHp));
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllHighestHpEnemies, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.CurrentHp == target.CombatState.Enemies.Where((Creature e) => e.IsAlive).Max((Creature e) => e.CurrentHp));
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.AnyFullLifeEnemy, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.CurrentHp == target.MaxHp);
		CustomTargetType.RegisterMultiTargetType(CustomTargetType.AllFullLifeEnemies, (Creature target) => target != null && target.IsAlive && target.IsEnemy && target.CurrentHp == target.MaxHp);
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.PetOrSelf, (Creature target, Player player) => (target.IsAlive && target.IsPet && target.PetOwner == player) || target == player.Creature);
		CustomTargetType.RegisterSingleTargetType(CustomTargetType.Pet, (Creature target, Player player) => target.IsAlive && target.IsPet && target.PetOwner == player);
	}
}
