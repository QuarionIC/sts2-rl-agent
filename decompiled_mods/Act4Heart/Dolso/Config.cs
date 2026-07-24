using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Dolso;

internal sealed record Config : IPacketSerializable
{
	[JsonSerializable(typeof(Config))]
	[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true, AllowTrailingCommas = true)]
	[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.14.31522")]
	internal class JsonSerializer : JsonSerializerContext, IJsonTypeInfoResolver
	{
		private JsonTypeInfo<bool>? _Boolean;

		private JsonTypeInfo<float>? _Single;

		private JsonTypeInfo<Config>? _Config;

		private JsonTypeInfo<SeBuff>? _SeBuff;

		private JsonTypeInfo<ushort>? _UInt16;

		private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			IncludeFields = true,
			WriteIndented = true
		};

		private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly JsonEncodedText PropName_super_elite_strength = JsonEncodedText.Encode("super_elite_strength");

		private static readonly JsonEncodedText PropName_super_elite_metallicize = JsonEncodedText.Encode("super_elite_metallicize");

		private static readonly JsonEncodedText PropName_super_elite_regenerate = JsonEncodedText.Encode("super_elite_regenerate");

		private static readonly JsonEncodedText PropName_super_elite_maxhp_percent = JsonEncodedText.Encode("super_elite_maxhp_percent");

		private static readonly JsonEncodedText PropName_keys_enable = JsonEncodedText.Encode("keys_enable");

		private static readonly JsonEncodedText PropName_spire_shield_orbs_focus_down_odds = JsonEncodedText.Encode("spire_shield_orbs_focus_down_odds");

		private static readonly JsonEncodedText PropName_heart_doom_damages_instead_of_kills = JsonEncodedText.Encode("heart_doom_damages_instead_of_kills");

		private static readonly JsonEncodedText PropName_multiplayer_heart_split_invincible_pool = JsonEncodedText.Encode("multiplayer_heart_split_invincible_pool");

		private static readonly JsonEncodedText PropName_multiplayer_heart_health_scaling_coef = JsonEncodedText.Encode("multiplayer_heart_health_scaling_coef");

		private static readonly JsonEncodedText PropName_multiplayer_act4_scaling_coef = JsonEncodedText.Encode("multiplayer_act4_scaling_coef");

		private static readonly JsonEncodedText PropName_act_mult = JsonEncodedText.Encode("act_mult");

		private static readonly JsonEncodedText PropName_add = JsonEncodedText.Encode("add");

		public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

		public JsonTypeInfo<float> Single => _Single ?? (_Single = (JsonTypeInfo<float>)base.Options.GetTypeInfo(typeof(float)));

		public JsonTypeInfo<Config> Config => _Config ?? (_Config = (JsonTypeInfo<Config>)base.Options.GetTypeInfo(typeof(Config)));

		public JsonTypeInfo<SeBuff> SeBuff => _SeBuff ?? (_SeBuff = (JsonTypeInfo<SeBuff>)base.Options.GetTypeInfo(typeof(SeBuff)));

		public JsonTypeInfo<ushort> UInt16 => _UInt16 ?? (_UInt16 = (JsonTypeInfo<ushort>)base.Options.GetTypeInfo(typeof(ushort)));

		public static JsonSerializer Default { get; } = new JsonSerializer(new JsonSerializerOptions(s_defaultOptions));

		protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

		private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool> jsonTypeInfo))
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter);
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private JsonTypeInfo<float> Create_Single(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<float> jsonTypeInfo))
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<float>(options, JsonMetadataServices.SingleConverter);
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private JsonTypeInfo<Config> Create_Config(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Config> jsonTypeInfo))
			{
				JsonObjectInfoValues<Config> objectInfo = new JsonObjectInfoValues<Config>
				{
					ObjectCreator = () => new Config(),
					ObjectWithParameterizedConstructorCreator = null,
					PropertyMetadataInitializer = (JsonSerializerContext _) => ConfigPropInit(options),
					ConstructorParameterMetadataInitializer = null,
					ConstructorAttributeProviderFactory = () => typeof(Config).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
					SerializeHandler = ConfigSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private static JsonPropertyInfo[] ConfigPropInit(JsonSerializerOptions options)
		{
			JsonPropertyInfo[] array = new JsonPropertyInfo[10];
			JsonPropertyInfoValues<SeBuff> propertyInfo = new JsonPropertyInfoValues<SeBuff>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).super_elite_strength,
				Setter = delegate(object obj, SeBuff value)
				{
					((Config)obj).super_elite_strength = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "super_elite_strength",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("super_elite_strength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			JsonPropertyInfoValues<SeBuff> propertyInfo2 = new JsonPropertyInfoValues<SeBuff>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).super_elite_metallicize,
				Setter = delegate(object obj, SeBuff value)
				{
					((Config)obj).super_elite_metallicize = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "super_elite_metallicize",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("super_elite_metallicize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
			JsonPropertyInfoValues<SeBuff> propertyInfo3 = new JsonPropertyInfoValues<SeBuff>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).super_elite_regenerate,
				Setter = delegate(object obj, SeBuff value)
				{
					((Config)obj).super_elite_regenerate = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "super_elite_regenerate",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("super_elite_regenerate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
			JsonPropertyInfoValues<ushort> propertyInfo4 = new JsonPropertyInfoValues<ushort>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).super_elite_maxhp_percent,
				Setter = delegate(object obj, ushort value)
				{
					((Config)obj).super_elite_maxhp_percent = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "super_elite_maxhp_percent",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("super_elite_maxhp_percent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
			JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).keys_enable,
				Setter = delegate(object obj, bool value)
				{
					((Config)obj).keys_enable = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "keys_enable",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("keys_enable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
			JsonPropertyInfoValues<float> propertyInfo6 = new JsonPropertyInfoValues<float>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).spire_shield_orbs_focus_down_odds,
				Setter = delegate(object obj, float value)
				{
					((Config)obj).spire_shield_orbs_focus_down_odds = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "spire_shield_orbs_focus_down_odds",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("spire_shield_orbs_focus_down_odds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
			JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).heart_doom_damages_instead_of_kills,
				Setter = delegate(object obj, bool value)
				{
					((Config)obj).heart_doom_damages_instead_of_kills = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "heart_doom_damages_instead_of_kills",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("heart_doom_damages_instead_of_kills", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
			JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).multiplayer_heart_split_invincible_pool,
				Setter = delegate(object obj, bool value)
				{
					((Config)obj).multiplayer_heart_split_invincible_pool = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "multiplayer_heart_split_invincible_pool",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("multiplayer_heart_split_invincible_pool", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
			JsonPropertyInfoValues<float> propertyInfo9 = new JsonPropertyInfoValues<float>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).multiplayer_heart_health_scaling_coef,
				Setter = delegate(object obj, float value)
				{
					((Config)obj).multiplayer_heart_health_scaling_coef = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "multiplayer_heart_health_scaling_coef",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("multiplayer_heart_health_scaling_coef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
			JsonPropertyInfoValues<float> propertyInfo10 = new JsonPropertyInfoValues<float>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(Config),
				Converter = null,
				Getter = (object obj) => ((Config)obj).multiplayer_act4_scaling_coef,
				Setter = delegate(object obj, float value)
				{
					((Config)obj).multiplayer_act4_scaling_coef = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "multiplayer_act4_scaling_coef",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(Config).GetField("multiplayer_act4_scaling_coef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
			return array;
		}

		private void ConfigSerializeHandler(Utf8JsonWriter writer, Config? value)
		{
			if ((object)value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			writer.WritePropertyName(PropName_super_elite_strength);
			SeBuffSerializeHandler(writer, value.super_elite_strength);
			writer.WritePropertyName(PropName_super_elite_metallicize);
			SeBuffSerializeHandler(writer, value.super_elite_metallicize);
			writer.WritePropertyName(PropName_super_elite_regenerate);
			SeBuffSerializeHandler(writer, value.super_elite_regenerate);
			writer.WriteNumber(PropName_super_elite_maxhp_percent, value.super_elite_maxhp_percent);
			writer.WriteBoolean(PropName_keys_enable, value.keys_enable);
			writer.WriteNumber(PropName_spire_shield_orbs_focus_down_odds, value.spire_shield_orbs_focus_down_odds);
			writer.WriteBoolean(PropName_heart_doom_damages_instead_of_kills, value.heart_doom_damages_instead_of_kills);
			writer.WriteBoolean(PropName_multiplayer_heart_split_invincible_pool, value.multiplayer_heart_split_invincible_pool);
			writer.WriteNumber(PropName_multiplayer_heart_health_scaling_coef, value.multiplayer_heart_health_scaling_coef);
			writer.WriteNumber(PropName_multiplayer_act4_scaling_coef, value.multiplayer_act4_scaling_coef);
			writer.WriteEndObject();
		}

		private JsonTypeInfo<SeBuff> Create_SeBuff(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<SeBuff> jsonTypeInfo))
			{
				JsonObjectInfoValues<SeBuff> objectInfo = new JsonObjectInfoValues<SeBuff>
				{
					ObjectCreator = () => default(SeBuff),
					ObjectWithParameterizedConstructorCreator = null,
					PropertyMetadataInitializer = (JsonSerializerContext _) => SeBuffPropInit(options),
					ConstructorParameterMetadataInitializer = null,
					ConstructorAttributeProviderFactory = () => typeof(SeBuff).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
					SerializeHandler = SeBuffSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private static JsonPropertyInfo[] SeBuffPropInit(JsonSerializerOptions options)
		{
			JsonPropertyInfo[] array = new JsonPropertyInfo[2];
			JsonPropertyInfoValues<float> propertyInfo = new JsonPropertyInfoValues<float>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(SeBuff),
				Converter = null,
				Getter = (object obj) => ((SeBuff)obj).act_mult,
				Setter = delegate(object obj, float value)
				{
					Unsafe.Unbox<SeBuff>(obj).act_mult = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "act_mult",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(SeBuff).GetField("act_mult", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
			JsonPropertyInfoValues<float> propertyInfo2 = new JsonPropertyInfoValues<float>
			{
				IsProperty = false,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(SeBuff),
				Converter = null,
				Getter = (object obj) => ((SeBuff)obj).add,
				Setter = delegate(object obj, float value)
				{
					Unsafe.Unbox<SeBuff>(obj).add = value;
				},
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "add",
				JsonPropertyName = null,
				AttributeProviderFactory = () => typeof(SeBuff).GetField("add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
			return array;
		}

		private void SeBuffSerializeHandler(Utf8JsonWriter writer, SeBuff value)
		{
			writer.WriteStartObject();
			writer.WriteNumber(PropName_act_mult, value.act_mult);
			writer.WriteNumber(PropName_add, value.add);
			writer.WriteEndObject();
		}

		private JsonTypeInfo<ushort> Create_UInt16(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ushort> jsonTypeInfo))
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ushort>(options, JsonMetadataServices.UInt16Converter);
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		public JsonSerializer()
			: base(null)
		{
		}

		public JsonSerializer(JsonSerializerOptions options)
			: base(options)
		{
		}

		private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
		{
			JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
			if (runtimeConverterForType != null)
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
				return true;
			}
			jsonTypeInfo = null;
			return false;
		}

		private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
		{
			for (int i = 0; i < options.Converters.Count; i++)
			{
				JsonConverter jsonConverter = options.Converters[i];
				if (jsonConverter != null && jsonConverter.CanConvert(type))
				{
					return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
				}
			}
			return null;
		}

		private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
		{
			if (validateCanConvert && !converter.CanConvert(type))
			{
				throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
			}
			if (converter is JsonConverterFactory jsonConverterFactory)
			{
				converter = jsonConverterFactory.CreateConverter(type, options);
				if (converter == null || converter is JsonConverterFactory)
				{
					throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
				}
			}
			return converter;
		}

		public override JsonTypeInfo? GetTypeInfo(Type type)
		{
			base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
			return typeInfo;
		}

		JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			if (type == typeof(bool))
			{
				return Create_Boolean(options);
			}
			if (type == typeof(float))
			{
				return Create_Single(options);
			}
			if (type == typeof(Config))
			{
				return Create_Config(options);
			}
			if (type == typeof(SeBuff))
			{
				return Create_SeBuff(options);
			}
			if (type == typeof(ushort))
			{
				return Create_UInt16(options);
			}
			return null;
		}
	}

	public SeBuff super_elite_strength = new SeBuff(1f, 0f);

	public SeBuff super_elite_metallicize = new SeBuff(2f, 2f);

	public SeBuff super_elite_regenerate = new SeBuff(2f, 1f);

	public ushort super_elite_maxhp_percent = 25;

	public bool keys_enable = true;

	public float spire_shield_orbs_focus_down_odds = 0.5f;

	public bool heart_doom_damages_instead_of_kills;

	public bool multiplayer_heart_split_invincible_pool;

	public float multiplayer_heart_health_scaling_coef = 1.4f;

	public float multiplayer_act4_scaling_coef = 1.4f;

	public void Deserialize(PacketReader reader)
	{
		super_elite_strength = reader.Read<SeBuff>();
		super_elite_metallicize = reader.Read<SeBuff>();
		super_elite_regenerate = reader.Read<SeBuff>();
		super_elite_maxhp_percent = reader.ReadUShort(16);
		keys_enable = reader.ReadBool();
		spire_shield_orbs_focus_down_odds = reader.ReadFloat((QuantizeParams?)null);
		heart_doom_damages_instead_of_kills = reader.ReadBool();
		multiplayer_heart_split_invincible_pool = reader.ReadBool();
		multiplayer_heart_health_scaling_coef = reader.ReadFloat((QuantizeParams?)null);
		multiplayer_act4_scaling_coef = reader.ReadFloat((QuantizeParams?)null);
	}

	public void Serialize(PacketWriter writer)
	{
		writer.Write<SeBuff>(super_elite_strength);
		writer.Write<SeBuff>(super_elite_metallicize);
		writer.Write<SeBuff>(super_elite_regenerate);
		writer.WriteUShort(super_elite_maxhp_percent, 16);
		writer.WriteBool(keys_enable);
		writer.WriteFloat(spire_shield_orbs_focus_down_odds, (QuantizeParams?)null);
		writer.WriteBool(heart_doom_damages_instead_of_kills);
		writer.WriteBool(multiplayer_heart_split_invincible_pool);
		writer.WriteFloat(multiplayer_heart_health_scaling_coef, (QuantizeParams?)null);
		writer.WriteFloat(multiplayer_act4_scaling_coef, (QuantizeParams?)null);
	}
}
