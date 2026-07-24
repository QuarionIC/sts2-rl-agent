using System;
using System.Collections.Generic;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public static class CustomPowerInstanceType
{
	[CustomEnum(null)]
	public static PowerInstanceType InstancedPerTarget;

	internal static readonly Dictionary<PowerInstanceType, Func<PowerModel, Creature, Creature?, PowerModel, bool>> PowerInstanceTypes = new Dictionary<PowerInstanceType, Func<PowerModel, Creature, Creature, PowerModel, bool>>();

	private static void RegisterPowerInstanceType(PowerInstanceType customType, Func<PowerModel, Creature, Creature?, PowerModel, bool> isPowerSame)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		DownfallMainFile.Logger.VeryDebug($"Registered power instance type {customType}", 1);
		PowerInstanceTypes.Add(customType, isPowerSame);
	}

	public static void RegisterAll()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		RegisterPowerInstanceType(InstancedPerTarget, (PowerModel model, Creature _, Creature? _, PowerModel otherPower) => model is IInstancedPerTarget instancedPerTarget && otherPower is IInstancedPerTarget instancedPerTarget2 && instancedPerTarget.TargetCreature == instancedPerTarget2.TargetCreature);
	}
}
