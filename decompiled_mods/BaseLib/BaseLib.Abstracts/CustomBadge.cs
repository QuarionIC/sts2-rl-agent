using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Abstracts;

public abstract class CustomBadge(bool requiresWin, bool multiplayerOnly)
{
	private static ConstructorInfo? MainBranchBadgeConstructor = typeof(Badge).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
	{
		typeof(SerializableRun),
		typeof(ulong)
	});

	public static readonly SpireField<Badge, string?> CustomBadgeIconPathDict = new SpireField<Badge, string>(() => (string?)null);

	public readonly bool RequiresWin = requiresWin;

	public readonly bool MultiplayerOnly = multiplayerOnly;

	private static ModuleBuilder? _moduleBuilder = null;

	private static readonly Dictionary<Type, Type> GeneratedBadges = new Dictionary<Type, Type>();

	public virtual string Id => GetType().GetPrefix() + StringExtensions.ToSnakeCase(GetType().Name).ToUpperInvariant();

	public virtual string? CustomBadgeIconPath => null;

	private static ModuleBuilder ModuleBuilder
	{
		get
		{
			if (_moduleBuilder == null)
			{
				_moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("BaseLibBadges"), AssemblyBuilderAccess.Run).DefineDynamicModule("GeneratedBadges");
			}
			return _moduleBuilder;
		}
	}

	public abstract BadgeRarity Rarity(SerializableRun run, SerializablePlayer player);

	public abstract bool IsObtained(SerializableRun run, SerializablePlayer player);

	public Badge ToRealBadge(SerializableRun run, bool won, ulong playerId)
	{
		Badge val = ((!(MainBranchBadgeConstructor == null)) ? GeneratedOldBadge(this, run, playerId) : GeneratedNewBadge(this, run, won, playerId));
		BaseLibMain.Logger.Info($"Setting custom badge path {CustomBadgeIconPath} for badge {val}", 1);
		CustomBadgeIconPathDict[val] = CustomBadgeIconPath;
		return val;
	}

	private static Badge GeneratedOldBadge(CustomBadge baseBadge, SerializableRun run, ulong playerId)
	{
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041e: Expected O, but got Unknown
		if (!GeneratedBadges.TryGetValue(baseBadge.GetType(), out Type value))
		{
			TypeBuilder typeBuilder = ModuleBuilder.DefineType(baseBadge.GetType().FullName + ".Generated", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit, typeof(Badge));
			FieldInfo field = AccessToolsExtensions.Field(typeof(Badge), "_run");
			FieldInfo field2 = AccessToolsExtensions.Field(typeof(Badge), "_localPlayer");
			MethodInfo meth = AccessToolsExtensions.Method(typeof(CustomBadge), "Rarity", (Type[])null, (Type[])null);
			MethodInfo meth2 = AccessToolsExtensions.Method(typeof(CustomBadge), "IsObtained", (Type[])null, (Type[])null);
			FieldBuilder field3 = typeBuilder.DefineField("baseBadge", typeof(CustomBadge), FieldAttributes.Public);
			ILGenerator iLGenerator = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis, new Type[3]
			{
				typeof(SerializableRun),
				typeof(ulong),
				typeof(CustomBadge)
			}).GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldarg_1);
			iLGenerator.Emit(OpCodes.Ldarg_2);
			iLGenerator.Emit(OpCodes.Call, MainBranchBadgeConstructor);
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldarg_3);
			iLGenerator.Emit(OpCodes.Stfld, field3);
			iLGenerator.Emit(OpCodes.Ret);
			MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("get_Id", attributes, typeof(string), Type.EmptyTypes);
			ILGenerator iLGenerator2 = methodBuilder.GetILGenerator();
			iLGenerator2.Emit(OpCodes.Ldstr, baseBadge.Id);
			iLGenerator2.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(methodBuilder, AccessToolsExtensions.Method(typeof(Badge), "get_Id", (Type[])null, (Type[])null));
			methodBuilder = typeBuilder.DefineMethod("get_RequiresWin", attributes, typeof(bool), Type.EmptyTypes);
			ILGenerator iLGenerator3 = methodBuilder.GetILGenerator();
			iLGenerator3.Emit(baseBadge.requiresWin ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			iLGenerator3.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(methodBuilder, AccessToolsExtensions.Method(typeof(Badge), "get_RequiresWin", (Type[])null, (Type[])null));
			methodBuilder = typeBuilder.DefineMethod("get_MultiplayerOnly", attributes, typeof(bool), Type.EmptyTypes);
			ILGenerator iLGenerator4 = methodBuilder.GetILGenerator();
			iLGenerator4.Emit(baseBadge.multiplayerOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			iLGenerator4.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(methodBuilder, AccessToolsExtensions.Method(typeof(Badge), "get_MultiplayerOnly", (Type[])null, (Type[])null));
			methodBuilder = typeBuilder.DefineMethod("get_Rarity", attributes, typeof(BadgeRarity), Type.EmptyTypes);
			ILGenerator iLGenerator5 = methodBuilder.GetILGenerator();
			iLGenerator5.Emit(OpCodes.Ldarg_0);
			iLGenerator5.Emit(OpCodes.Ldfld, field3);
			iLGenerator5.Emit(OpCodes.Ldarg_0);
			iLGenerator5.Emit(OpCodes.Ldfld, field);
			iLGenerator5.Emit(OpCodes.Ldarg_0);
			iLGenerator5.Emit(OpCodes.Ldfld, field2);
			iLGenerator5.Emit(OpCodes.Callvirt, meth);
			iLGenerator5.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(methodBuilder, AccessToolsExtensions.Method(typeof(Badge), "get_Rarity", (Type[])null, (Type[])null));
			MethodBuilder methodBuilder2 = typeBuilder.DefineMethod("IsObtained", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), Type.EmptyTypes);
			ILGenerator iLGenerator6 = methodBuilder2.GetILGenerator();
			iLGenerator6.Emit(OpCodes.Ldarg_0);
			iLGenerator6.Emit(OpCodes.Ldfld, field3);
			iLGenerator6.Emit(OpCodes.Ldarg_0);
			iLGenerator6.Emit(OpCodes.Ldfld, field);
			iLGenerator6.Emit(OpCodes.Ldarg_0);
			iLGenerator6.Emit(OpCodes.Ldfld, field2);
			iLGenerator6.Emit(OpCodes.Callvirt, meth2);
			iLGenerator6.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(methodBuilder2, AccessToolsExtensions.Method(typeof(Badge), "IsObtained", (Type[])null, (Type[])null));
			value = typeBuilder.CreateType();
			GeneratedBadges[baseBadge.GetType()] = value;
			BaseLibMain.Logger.Info("Generated main branch badge type for " + baseBadge.Id, 1);
		}
		return (Badge)Activator.CreateInstance(value, run, playerId, baseBadge);
	}

	private static Badge GeneratedNewBadge(CustomBadge baseBadge, SerializableRun run, bool won, ulong playerId)
	{
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected O, but got Unknown
		if (!GeneratedBadges.TryGetValue(baseBadge.GetType(), out Type value))
		{
			TypeBuilder typeBuilder = ModuleBuilder.DefineType(baseBadge.GetType().FullName + ".Generated", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit, typeof(DynamicBadge));
			ConstructorInfo constructor = typeof(DynamicBadge).GetConstructor(new Type[4]
			{
				typeof(CustomBadge),
				typeof(SerializableRun),
				typeof(bool),
				typeof(ulong)
			});
			ILGenerator iLGenerator = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis, new Type[4]
			{
				typeof(SerializableRun),
				typeof(bool),
				typeof(ulong),
				typeof(CustomBadge)
			}).GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldarg_S, 4);
			iLGenerator.Emit(OpCodes.Ldarg_1);
			iLGenerator.Emit(OpCodes.Ldarg_2);
			iLGenerator.Emit(OpCodes.Ldarg_3);
			iLGenerator.Emit(OpCodes.Call, constructor);
			iLGenerator.Emit(OpCodes.Ret);
			value = typeBuilder.CreateType();
			GeneratedBadges[baseBadge.GetType()] = value;
			BaseLibMain.Logger.Info("Generated beta branch badge type for " + baseBadge.Id, 1);
		}
		return (Badge)Activator.CreateInstance(value, run, won, playerId, baseBadge);
	}
}
