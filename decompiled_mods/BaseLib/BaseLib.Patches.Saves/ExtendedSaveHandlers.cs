using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Extensions;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace BaseLib.Patches.Saves;

public static class ExtendedSaveHandlers<DataType, SerializableType> where SerializableType : class
{
	public class ExtendedSaveData
	{
		public readonly Dictionary<Type, IDictionary> Dictionaries = new Dictionary<Type, IDictionary>();

		public Dictionary<string, T> DictForType<T>()
		{
			if (!Dictionaries.TryGetValue(typeof(T), out IDictionary value))
			{
				if (!Dictionaries.TryAdd(typeof(T), value = new Dictionary<string, T>()))
				{
					throw new Exception("Failed to add missing type to dictionary");
				}
				return (Dictionary<string, T>)value;
			}
			return (Dictionary<string, T>)value;
		}

		public ExtendedSaveData(DataType data)
		{
			foreach (ExtendedSaveInfo<DataType, ExtendedSaveData> registeredSafe in ExtendedSaveHandlers<DataType, SerializableType>.RegisteredSaves)
			{
				registeredSafe.Getter(data, this);
			}
		}

		public ExtendedSaveData()
		{
		}
	}

	private static NotNullSpireField<SerializableType, ExtendedSaveData>? _extendedData;

	private static List<ExtendedSaveInfo<DataType, ExtendedSaveData>>? _registeredSaves;

	private static Dictionary<Type, Func<JsonSerializerOptions, JsonPropertyInfo>>? _saveValueTypes;

	private static bool _initializedSaveProps;

	public static NotNullSpireField<SerializableType, ExtendedSaveData> ExtendedData => _extendedData ?? (_extendedData = new NotNullSpireField<SerializableType, ExtendedSaveData>(() => new ExtendedSaveData()));

	public static List<ExtendedSaveInfo<DataType, ExtendedSaveData>> RegisteredSaves => _registeredSaves ?? (_registeredSaves = new List<ExtendedSaveInfo<DataType, ExtendedSaveData>>());

	private static Dictionary<Type, Func<JsonSerializerOptions, JsonPropertyInfo>> SaveValueTypes => _saveValueTypes ?? (_saveValueTypes = new Dictionary<Type, Func<JsonSerializerOptions, JsonPropertyInfo>>());

	public static void RegisterSave<T>(string id, Func<DataType, T?> getter, Action<DataType, T?> setter) where T : IPacketSerializable, new()
	{
		ExtendedSaveHandlers<DataType, SerializableType>.RegisterSave<T>(id, getter, setter, (Action<T, PacketWriter>)delegate(T val, PacketWriter writer)
		{
			((IPacketSerializable)val/*cast due to .constrained prefix*/).Serialize(writer);
		}, (Func<PacketReader, T>)delegate(PacketReader reader)
		{
			T result = new T();
			((IPacketSerializable)result/*cast due to .constrained prefix*/).Deserialize(reader);
			return result;
		});
	}

	public static void RegisterSave<TargetType, T>(string id, Func<TargetType, T?> getter, Action<TargetType, T?> setter, Action<T, PacketWriter> serializer, Func<PacketReader, T> deserializer)
	{
		ExtendedSaveHandlers<DataType, SerializableType>.RegisterSave<T>(id, (Func<DataType, T?>)((DataType enchant) => (enchant is TargetType arg) ? getter(arg) : default(T)), (Action<DataType, T?>)delegate(DataType enchant, T? val)
		{
			if (enchant is TargetType arg)
			{
				setter(arg, val);
			}
		}, serializer, deserializer);
	}

	public static void RegisterSave<T>(string id, Func<DataType, T?> getter, Action<DataType, T?> setter, Action<T, PacketWriter> serializer, Func<PacketReader, T> deserializer)
	{
		ExtendedSaveTypes.RegisterDictionarySaveType<string, T>();
		if (!SaveValueTypes.ContainsKey(typeof(T)))
		{
			if (_initializedSaveProps)
			{
				BaseLibMain.Logger.Warn($"Saved types for {typeof(SerializableType).Name} have already been registered; registered save values of type {typeof(T).Name} will not be saved.", 1);
			}
			SaveValueTypes.Add(typeof(T), (JsonSerializerOptions options) => JsonMetadataServices.CreatePropertyInfo(options, SavePatchUtils.QuickProps("save_dict_" + MakeTypeName(typeof(T)), (SerializableType obj) => ExtendedData[obj].DictForType<T>(), delegate(SerializableType obj, Dictionary<string, T>? value)
			{
				if (value == null)
				{
					value = new Dictionary<string, T>();
				}
				ExtendedData[obj].Dictionaries[typeof(T)] = value;
			})));
		}
		RegisteredSaves.InsertSorted(new ExtendedSaveInfo<DataType, ExtendedSaveData>(id, delegate(DataType model, ExtendedSaveData data)
		{
			T val = getter(model);
			if (val != null && !data.DictForType<T>().TryAdd(id, val))
			{
				BaseLibMain.Logger.Error($"Duplicate {typeof(DataType).Name} save key: [{typeof(T).Name}] {id}", 1);
			}
		}, delegate(DataType model, ExtendedSaveData data)
		{
			if (data.DictForType<T>().TryGetValue(id, out var value))
			{
				setter(model, value);
			}
		}, delegate(ExtendedSaveData data, PacketWriter writer)
		{
			T valueOrDefault = data.DictForType<T>().GetValueOrDefault(id);
			if (valueOrDefault == null)
			{
				writer.WriteBool(false);
			}
			else
			{
				writer.WriteBool(true);
				serializer(valueOrDefault, writer);
			}
		}, delegate(ExtendedSaveData data, PacketReader reader)
		{
			if (reader.ReadBool())
			{
				T val = deserializer(reader);
				if (val != null)
				{
					data.DictForType<T>()[id] = val;
				}
			}
		}));
	}

	private static string MakeTypeName(Type t)
	{
		string shortName = GetShortName(t);
		if (t.IsGenericType)
		{
			return shortName + "[" + GeneralExtensions.Join<Type>((IEnumerable<Type>)t.GenericTypeArguments, (Func<Type, string>)MakeTypeName, ",") + "]";
		}
		return shortName ?? "";
	}

	private static string GetShortName(Type t)
	{
		if (t.IsAssignableTo(typeof(IList)))
		{
			return "List";
		}
		if (t.IsAssignableTo(typeof(IDictionary)))
		{
			return "Dictionary";
		}
		if (t.FullName != null && !t.FullName.StartsWith("System"))
		{
			return t.FullName;
		}
		return t.Name;
	}

	public static IEnumerable<JsonPropertyInfo> CreateExtendedProperties(JsonSerializerOptions options)
	{
		BaseLibMain.Logger.Info("Adding custom save data to " + typeof(SerializableType).Name + ".", 1);
		_initializedSaveProps = true;
		foreach (KeyValuePair<Type, Func<JsonSerializerOptions, JsonPropertyInfo>> saveValueType in SaveValueTypes)
		{
			yield return saveValueType.Value(options);
		}
	}

	public static void Load(SerializableType dataSource, DataType holder)
	{
		ExtendedSaveData arg = ExtendedData[dataSource];
		foreach (ExtendedSaveInfo<DataType, ExtendedSaveData> registeredSafe in RegisteredSaves)
		{
			registeredSafe.Setter(holder, arg);
		}
	}

	public static void Write(SerializableType dataSource, PacketWriter writer)
	{
		if (!PostModInitPatch.CanModifyGameplay)
		{
			return;
		}
		ExtendedSaveData arg = ExtendedData[dataSource];
		foreach (ExtendedSaveInfo<DataType, ExtendedSaveData> registeredSafe in RegisteredSaves)
		{
			registeredSafe.Serializer(arg, writer);
		}
	}

	public static void Read(SerializableType dataSource, PacketReader reader)
	{
		if (!PostModInitPatch.CanModifyGameplay)
		{
			return;
		}
		ExtendedSaveData arg = ExtendedData[dataSource];
		foreach (ExtendedSaveInfo<DataType, ExtendedSaveData> registeredSafe in RegisteredSaves)
		{
			registeredSafe.Deserializer(arg, reader);
		}
	}
}
