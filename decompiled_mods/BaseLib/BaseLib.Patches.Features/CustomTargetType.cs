using System;
using System.Collections.Generic;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BaseLib.Patches.Features;

public static class CustomTargetType
{
	[CustomEnum(null)]
	public static TargetType Everyone;

	[CustomEnum(null)]
	public static TargetType Anyone;

	[CustomEnum(null)]
	public static TargetType AllAttackingEnemies;

	[CustomEnum(null)]
	public static TargetType AnyAttackingEnemy;

	[CustomEnum(null)]
	public static TargetType AllBlockingEnemies;

	[CustomEnum(null)]
	public static TargetType AnyBlockingEnemy;

	[CustomEnum(null)]
	public static TargetType AllNonBlockingEnemies;

	[CustomEnum(null)]
	public static TargetType AnyNonBlockingEnemy;

	[CustomEnum(null)]
	public static TargetType AllHighestHpEnemies;

	[CustomEnum(null)]
	public static TargetType AllLowestHpEnemies;

	[CustomEnum(null)]
	public static TargetType AnyFullLifeEnemy;

	[CustomEnum(null)]
	public static TargetType AllFullLifeEnemies;

	[CustomEnum(null)]
	public static TargetType PetOrSelf;

	[CustomEnum(null)]
	public static TargetType Pet;

	internal static readonly Dictionary<TargetType, Func<Creature, Player, bool>> SingleTargeting = new Dictionary<TargetType, Func<Creature, Player, bool>>();

	internal static readonly Dictionary<TargetType, Func<Creature, Player, bool>> MultiTargeting = new Dictionary<TargetType, Func<Creature, Player, bool>>();

	public static bool CanMultiTarget(TargetType targetType, Creature creature, Player player)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		MultiTargeting.TryGetValue(targetType, out Func<Creature, Player, bool> value);
		return value?.Invoke(creature, player) ?? false;
	}

	public static bool IsCustomSingleTargetType(TargetType targetType)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return SingleTargeting.ContainsKey(targetType);
	}

	public static bool IsCustomMultiTargetType(TargetType targetType)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return MultiTargeting.ContainsKey(targetType);
	}

	public static void RegisterSingleTargetType(TargetType customType, Func<Creature, bool> canTarget)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		RegisterSingleTargetType(customType, (Creature creature, Player _) => canTarget(creature));
	}

	public static void RegisterSingleTargetType(TargetType customType, Func<Creature, Player, bool> canTarget)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		BaseLibMain.Logger.VeryDebug($"Registered single target type {customType}", 1);
		SingleTargeting.Add(customType, canTarget);
	}

	public static void RegisterMultiTargetType(TargetType customType, Func<Creature, bool>? showReticleFor = null)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		RegisterMultiTargetType(customType, (showReticleFor != null) ? ((Func<Creature, Player, bool>)((Creature creature, Player _) => showReticleFor(creature))) : null);
	}

	public static void RegisterMultiTargetType(TargetType customType, Func<Creature, Player, bool>? showReticleFor = null)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		BaseLibMain.Logger.VeryDebug($"Registered multi target type {customType}", 1);
		MultiTargeting.Add(customType, showReticleFor ?? ((Func<Creature, Player, bool>)((Creature _, Player _) => true)));
	}
}
