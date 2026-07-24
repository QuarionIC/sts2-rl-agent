using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(SavedProperties))]
internal static class SavedSpireFieldPatch
{
	private static readonly List<ISavedSpireField> RegisteredFields = new List<ISavedSpireField>();

	private static Type? _targetType;

	private static bool _beta = false;

	public static void Register<TKey, TVal>(SavedSpireField<TKey, TVal> field) where TKey : class
	{
		RegisteredFields.Add(field);
	}

	private static IEnumerable<ISavedSpireField> GetFieldsForModel(object model)
	{
		return RegisteredFields.Where((ISavedSpireField f) => f.TargetType.IsInstanceOfType(model));
	}

	[HarmonyPatch("FromInternal")]
	[HarmonyPostfix]
	private static void PostfixFromInternal(ref SavedProperties? __result, object model)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		SavedProperties val = (SavedProperties)(((object)__result) ?? ((object)new SavedProperties()));
		bool flag = false;
		foreach (ISavedSpireField item in GetFieldsForModel(model))
		{
			item.Export(model, val);
			flag = true;
		}
		if (__result == null && flag)
		{
			__result = val;
		}
	}

	[HarmonyPatch("FillInternal")]
	[HarmonyPostfix]
	private static void PostfixFillInternal(SavedProperties __instance, object model)
	{
		foreach (ISavedSpireField item in GetFieldsForModel(model))
		{
			item.Import(model, __instance);
		}
	}

	internal static void AddFieldsSorted()
	{
		BaseLibMain.Logger.Info($"Found {RegisteredFields.Count} SavedSpireFields.", 1);
		RegisteredFields.Sort((ISavedSpireField a, ISavedSpireField b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
		foreach (ISavedSpireField registeredField in RegisteredFields)
		{
			if (registeredField.IsBasegameSupported)
			{
				InjectNameIntoBaseGameCache(registeredField.Name);
			}
			else if (!registeredField.RegisterCustomSave())
			{
				BaseLibMain.Logger.Error("SavedSpireField " + registeredField.Name + " will not be saved as it is of an unsupported type.", 1);
			}
		}
	}

	private static void InjectNameIntoBaseGameCache(string name)
	{
		if (_targetType == null)
		{
			_targetType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache");
			if (_targetType == null)
			{
				_targetType = typeof(ModelIdSerializationCache);
				_beta = true;
			}
		}
		Dictionary<string, int> dictionary = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(_targetType, "_propertyNameToNetIdMap");
		List<string> list = AccessTools.StaticFieldRefAccess<List<string>>(_targetType, "_netIdToPropertyNameMap");
		MethodInfo methodInfo = AccessTools.PropertySetter(_targetType, _beta ? "PropertyIdBitSize" : "NetIdBitSize");
		if (!dictionary.ContainsKey(name))
		{
			dictionary[name] = list.Count;
			list.Add(name);
			BaseLibMain.Logger.Debug($"Added saved property name to basegame cache: {name} => {dictionary[name]}", 1);
			int num = Mathf.CeilToInt(Math.Log2(list.Count));
			methodInfo.Invoke(null, new object[1] { num });
		}
		else
		{
			BaseLibMain.Logger.Error("SavedSpireField name is not unique: " + name, 1);
		}
	}
}
