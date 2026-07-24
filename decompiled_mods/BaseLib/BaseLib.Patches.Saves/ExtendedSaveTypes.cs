using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Saves;

[HarmonyPatch(typeof(MegaCritSerializerContext), "global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver.GetTypeInfo")]
public class ExtendedSaveTypes
{
	private static readonly Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>> ExtendedTypes = new Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>>();

	[HarmonyPostfix]
	private static void GetExtraType(MegaCritSerializerContext __instance, Type type, JsonSerializerOptions options, ref JsonTypeInfo? __result)
	{
		if (__result == null)
		{
			BaseLibMain.Logger.Debug($"Type {type} missing for serialization, checking extended types", 1);
			if (ExtendedTypes.TryGetValue(type, out Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo> value))
			{
				__result = value((IJsonTypeInfoResolver)__instance, options);
			}
		}
	}

	public static bool IsSaveTypeSupported(Type t)
	{
		return ((JsonSerializerContext)(object)MegaCritSerializerContext.Default).GetTypeInfo(t) != null;
	}

	public static bool IsSaveHolderSupported(Type t)
	{
		if (!t.IsAssignableTo(typeof(CardModel)) && !t.IsAssignableTo(typeof(RelicModel)) && !t.IsAssignableTo(typeof(PotionModel)) && !t.IsAssignableTo(typeof(EnchantmentModel)) && !t.IsAssignableTo(typeof(Player)) && !t.IsAssignableTo(typeof(Reward)))
		{
			return t.IsAssignableTo(typeof(IRunState));
		}
		return true;
	}

	public static bool RegisterSavedValue<TargetType, T>(string id, Func<TargetType, T?> getter, Action<TargetType, T?> setter, Action<T, PacketWriter> serializer, Func<PacketReader, T> deserializer)
	{
		Type typeFromHandle = typeof(TargetType);
		if (typeFromHandle.IsAssignableTo(typeof(CardModel)))
		{
			ExtendedSaveHandlers<CardModel, SerializableCard>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(RelicModel)))
		{
			ExtendedSaveHandlers<RelicModel, SerializableRelic>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(PotionModel)))
		{
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(EnchantmentModel)))
		{
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(Player)))
		{
			ExtendedSaveHandlers<Player, SerializablePlayer>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(Reward)))
		{
			ExtendedSaveHandlers<Reward, SerializableReward>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		if (typeFromHandle.IsAssignableTo(typeof(IRunState)))
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.RegisterSave(id, getter, setter, serializer, deserializer);
			return true;
		}
		BaseLibMain.Logger.Warn($"Could not register saved value {id}; type {typeof(TargetType).Name} is not set up in ExtendedSaveTypes.RegisterSavedValue", 1);
		return false;
	}

	public static void RegisterAdditionalSaveType<T>(Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo> typeInfoFunc)
	{
		if (!ExtendedTypes.ContainsKey(typeof(T)))
		{
			ExtendedTypes[typeof(T)] = typeInfoFunc;
		}
	}

	public static void RegisterObjectSaveType<T>(params Func<JsonSerializerOptions, JsonPropertyInfo>[] dataFunctions) where T : notnull, new()
	{
		if (ExtendedTypes.ContainsKey(typeof(T)))
		{
			return;
		}
		RegisterAdditionalSaveType<T>(delegate(IJsonTypeInfoResolver resolver, JsonSerializerOptions options)
		{
			JsonObjectInfoValues<T> objectInfo = new JsonObjectInfoValues<T>
			{
				ObjectCreator = () => new T(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => dataFunctions.Select((Func<JsonSerializerOptions, JsonPropertyInfo> func) => func(options)).ToArray(),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = null
			};
			JsonTypeInfo<T> jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
			jsonTypeInfo.OriginatingResolver = resolver;
			return jsonTypeInfo;
		});
	}

	public static void RegisterDictionarySaveType<TKey, TValue>() where TKey : notnull
	{
		if (ExtendedTypes.ContainsKey(typeof(Dictionary<TKey, TValue>)))
		{
			return;
		}
		RegisterAdditionalSaveType<Dictionary<TKey, TValue>>(delegate(IJsonTypeInfoResolver resolver, JsonSerializerOptions options)
		{
			JsonCollectionInfoValues<Dictionary<TKey, TValue>> collectionInfo = new JsonCollectionInfoValues<Dictionary<TKey, TValue>>
			{
				ObjectCreator = () => new Dictionary<TKey, TValue>(),
				SerializeHandler = null
			};
			JsonTypeInfo<Dictionary<TKey, TValue>> jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<TKey, TValue>, TKey, TValue>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
			jsonTypeInfo.OriginatingResolver = resolver;
			return jsonTypeInfo;
		});
	}

	public static void RegisterListSaveType<TValue>()
	{
		if (ExtendedTypes.ContainsKey(typeof(List<TValue>)))
		{
			return;
		}
		RegisterAdditionalSaveType<List<TValue>>(delegate(IJsonTypeInfoResolver resolver, JsonSerializerOptions options)
		{
			JsonTypeInfo<List<TValue>> jsonTypeInfo = default(JsonTypeInfo<List<TValue>>);
			if (!MegaCritSerializerContext.TryGetTypeInfoForRuntimeCustomConverter<List<TValue>>(options, ref jsonTypeInfo))
			{
				JsonCollectionInfoValues<List<TValue>> collectionInfo = new JsonCollectionInfoValues<List<TValue>>
				{
					ObjectCreator = () => new List<TValue>(),
					SerializeHandler = null
				};
				jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<TValue>, TValue>(options, collectionInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = resolver;
			return jsonTypeInfo;
		});
	}

	public static Func<JsonSerializerOptions, JsonPropertyInfo> PropertyFunc<DeclaringType, PropType>(string propName)
	{
		PropertyInfo prop = typeof(DeclaringType).GetProperty(propName);
		if (prop == null)
		{
			throw new ArgumentException("Unable to find public property '" + propName + "' in type " + typeof(DeclaringType).Name);
		}
		return delegate(JsonSerializerOptions options)
		{
			JsonPropertyInfoValues<PropType> propertyInfo = new JsonPropertyInfoValues<PropType>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(DeclaringType),
				Converter = null,
				Getter = (object obj) => (PropType)prop.GetValue(obj),
				Setter = delegate(object obj, PropType? value)
				{
					prop.SetValue(obj, value);
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = propName,
				JsonPropertyName = null,
				AttributeProviderFactory = () => prop
			};
			JsonPropertyInfo jsonPropertyInfo = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			jsonPropertyInfo.IsGetNullable = false;
			jsonPropertyInfo.IsSetNullable = false;
			return jsonPropertyInfo;
		};
	}

	public static Func<JsonSerializerOptions, JsonPropertyInfo> FieldFunc<DeclaringType, FieldType>(string fieldName)
	{
		FieldInfo field = typeof(DeclaringType).GetField(fieldName);
		if (field == null)
		{
			throw new ArgumentException("Unable to find public field '" + fieldName + "' in type " + typeof(DeclaringType).Name);
		}
		return delegate(JsonSerializerOptions options)
		{
			JsonPropertyInfoValues<FieldType> propertyInfo = new JsonPropertyInfoValues<FieldType>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(DeclaringType),
				Converter = null,
				Getter = (object obj) => (FieldType)field.GetValue(obj),
				Setter = delegate(object obj, FieldType? value)
				{
					field.SetValue(obj, value);
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = fieldName,
				JsonPropertyName = null,
				AttributeProviderFactory = () => field
			};
			JsonPropertyInfo jsonPropertyInfo = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			jsonPropertyInfo.IsGetNullable = false;
			jsonPropertyInfo.IsSetNullable = false;
			return jsonPropertyInfo;
		};
	}
}
