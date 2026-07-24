using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Patches.Features;
using BaseLib.Patches.Saves;
using BaseLib.Patches.Utils;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using SmartFormat.Core.Extensions;

namespace BaseLib.Patches;

[HarmonyPatch]
internal class PostModInitPatch
{
	[HarmonyPatch(typeof(ModelDb), "Preload")]
	private class RegisterSceneConversions
	{
		[HarmonyPostfix]
		private static void EnsureScenePathsRegistered()
		{
			Type[] modTypes = ReflectionHelper.ModTypes;
			foreach (Type type in modTypes)
			{
				if ((object)type != null && !type.IsAbstract && !type.IsInterface && type.IsAssignableTo(typeof(AbstractModel)) && type.IsAssignableTo(typeof(ISceneConversions)))
				{
					(ModelDb.GetById<AbstractModel>(ModelDb.GetId(type)) as ISceneConversions)?.RegisterSceneConversions();
				}
			}
		}
	}

	private static bool _earlyInit = false;

	private static bool _lateInit = false;

	private static readonly List<IFormatter> AddLaterFormatters = new List<IFormatter>();

	public static bool CanModifyGameplay { get; private set; } = false;

	[HarmonyPatch(typeof(LocManager), "Initialize")]
	[HarmonyPrefix]
	private static void EarlyPostInit()
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		if (_earlyInit)
		{
			return;
		}
		_earlyInit = true;
		BaseLibMain.Logger.Info("Performing early post-mod init", 1);
		WhatMod.BuildAfterInit();
		foreach (Mod loadedMod in ModManager.GetLoadedMods())
		{
			ModManifest manifest = loadedMod.manifest;
			if (manifest != null && manifest.affectsGameplay && BetaMainCompatibility._ModManifest.HasDependency(loadedMod.manifest, "BaseLib"))
			{
				BaseLibMain.Logger.Info("Mod " + loadedMod.manifest.id + " that modifies gameplay has BaseLib dependency; gameplay modification enabled.", 1);
				CanModifyGameplay = true;
				break;
			}
		}
		if (CanModifyGameplay)
		{
			CardModifier.RegisterSave();
		}
		CustomMessageWrapper.Initialize();
		CustomTargetedMessageWrapper.Initialize();
		Harmony harmony = new Harmony("PostModInit");
		AddActContent.Patch(harmony);
		ModInterop modInterop = new ModInterop();
		Type[] modTypes = ReflectionHelper.ModTypes;
		foreach (Type type in modTypes)
		{
			modInterop.ProcessType(harmony, type);
			if (type.IsAbstract || type.IsInterface)
			{
				continue;
			}
			if (type.IsAssignableTo(typeof(CustomResource)))
			{
				try
				{
					MethodInfo method = typeof(CustomResources<>).MakeGenericType(type).GetMethod("Register", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (method == null)
					{
						BaseLibMain.Logger.Warn($"Failed to get registration method for custom resource type {type}", 1);
					}
					else if (!(Activator.CreateInstance(type) is CustomResource customResource))
					{
						BaseLibMain.Logger.Warn($"Failed to initialize custom resource type {type}", 1);
					}
					else
					{
						BaseLibMain.Logger.Info("Registering custom resource " + type.Name, 1);
						method.Invoke(null, new object[1] { customResource });
					}
				}
				catch (Exception value)
				{
					BaseLibMain.Logger.Error($"Exception occurred registering custom resource {type}; {value}", 1);
				}
			}
			if (!type.IsAssignableTo(typeof(IAutoRegisterFormatSpecifier)))
			{
				continue;
			}
			try
			{
				object? obj = Activator.CreateInstance(type);
				IFormatter val = (IFormatter)((obj is IFormatter) ? obj : null);
				if (val != null)
				{
					AddLaterFormatters.Add(val);
					BaseLibMain.Logger.Info("Instantiated custom format specifier " + type.Name + " to add later", 1);
				}
				else
				{
					BaseLibMain.Logger.Warn($"Failed to initialize IAutoRegisterFormatSpecifier type {type}", 1);
				}
			}
			catch (Exception value2)
			{
				BaseLibMain.Logger.Error($"Exception occurred adding format specifier {type}; {value2}", 1);
			}
		}
	}

	[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
	[HarmonyPostfix]
	private static void AddFormattersOnLocInit(LocManager __instance)
	{
		if (AddLaterFormatters.Count != 0)
		{
			BaseLibMain.Logger.Info($"Added {AddLaterFormatters.Count} formatters after LoadLocFormatters.", 1);
			LocManager._smartFormatter.AddExtensions(AddLaterFormatters.ToArray());
		}
	}

	[HarmonyPatch(typeof(ModelDb), "InitIds")]
	[HarmonyPrefix]
	private static void LatePostInit()
	{
		if (_lateInit)
		{
			return;
		}
		_lateInit = true;
		BaseLibMain.Logger.Info("Performing late post-mod init", 1);
		Type[] modTypes = ReflectionHelper.ModTypes;
		foreach (Type type in modTypes)
		{
			bool flag = false;
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (((MemberInfo)propertyInfo).GetCustomAttribute<SavedPropertyAttribute>() != null && !(propertyInfo.DeclaringType == null))
				{
					if (!SavePatchUtils.IsStoreTypeBaseSupported(propertyInfo.PropertyType))
					{
						BaseLibMain.Logger.Warn($"SavedProperty does not support values of type {propertyInfo.PropertyType}; change {type.Name}.{propertyInfo.Name} to a SavedSpireField for BaseLib to save it.", 1);
					}
					else if (!SavePatchUtils.IsHolderTypeBaseSupported(propertyInfo.DeclaringType))
					{
						string value = (ExtendedSaveTypes.IsSaveHolderSupported(type) ? "change to a SavedSpireField for BaseLib to save it." : "this type is currently also unsupported by BaseLib for saved values.");
						BaseLibMain.Logger.Warn($"SavedProperty {propertyInfo.Name} will not work on type {type.Name}; {value}", 1);
					}
					else
					{
						flag = true;
					}
				}
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			for (int j = 0; j < fields.Length; j++)
			{
				CheckSpecialSpireField(fields[j]);
			}
			if (flag)
			{
				BetaMainCompatibility.CacheSavedProperties(type);
			}
		}
		SavedSpireFieldPatch.AddFieldsSorted();
	}

	private static void CheckSpecialSpireField(FieldInfo field)
	{
		Type fieldType = field.FieldType;
		if (fieldType.IsGenericType)
		{
			Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();
			if (!(genericTypeDefinition != typeof(SavedSpireField<, >)) || !(genericTypeDefinition != typeof(AddedNode<, >)))
			{
				field.GetValue(null);
			}
		}
	}
}
