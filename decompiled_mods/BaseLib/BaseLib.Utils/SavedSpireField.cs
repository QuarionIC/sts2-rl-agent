using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Patches.Saves;
using BaseLib.Patches.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Utils;

public class SavedSpireField<TKey, TVal> : SpireField<TKey, TVal>, ISavedSpireField where TKey : class
{
	public bool IsBasegameSupported { get; init; }

	public string Name { get; }

	public Type TargetType { get; } = typeof(TKey);

	public Action<TVal, PacketWriter>? Serializer { get; set; }

	public Func<PacketReader, TVal>? Deserializer { get; set; }

	public SavedSpireField(Func<TVal?> defaultVal, string name)
		: this((Func<TKey, TVal?>)((TKey _) => defaultVal()), name)
	{
	}

	public SavedSpireField(Func<TKey, TVal?> defaultVal, string name)
		: base(defaultVal)
	{
		string name2 = typeof(TKey).Name;
		Name = name2 + "_" + name;
		if (!SavePatchUtils.IsStoreTypeBaseSupported(typeof(TVal)) || !SavePatchUtils.IsHolderTypeBaseSupported(typeof(TKey)))
		{
			IsBasegameSupported = false;
		}
		else
		{
			IsBasegameSupported = true;
		}
		SavedSpireFieldPatch.Register(this);
	}

	public void Export(object model, SavedProperties props)
	{
		AddToProperties(props, Name, Get((TKey)model));
	}

	public void Import(object model, SavedProperties props)
	{
		if (TryGetFromProperties<TVal>(props, Name, out TVal value))
		{
			Set((TKey)model, value);
		}
	}

	public bool RegisterCustomSave()
	{
		Action<TVal, PacketWriter> serializer = Serializer;
		Func<PacketReader, TVal> deserializer = Deserializer;
		if (serializer == null || deserializer == null)
		{
			if (typeof(TVal).IsAssignableTo(typeof(IPacketSerializable)))
			{
				serializer = delegate(TVal val, PacketWriter writer)
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					((IPacketSerializable)(object)val).Serialize(writer);
				};
				deserializer = delegate(PacketReader reader)
				{
					//IL_001a: Unknown result type (might be due to invalid IL or missing references)
					TVal obj = (TVal)AccessToolsExtensions.CreateInstance(typeof(TVal));
					((IPacketSerializable)(object)obj).Deserialize(reader);
					return obj;
				};
			}
			else if (!SavePatchUtils.TryGetSerializerDeserializer(out serializer, out deserializer))
			{
				BaseLibMain.Logger.Error($"Unable to register custom save for SavedSpireField {Name}; no serialization defined for type {typeof(TVal).Name}. Set Serializer/Deserializer properties of SavedSpireField.", 1);
				return false;
			}
		}
		if (!ExtendedSaveTypes.IsSaveTypeSupported(typeof(TVal)))
		{
			throw new ArgumentException("Type " + typeof(TVal).Name + " is not registered for saving; register the type with ExtendedSaveTypes in your mod's initializer.");
		}
		return ExtendedSaveTypes.RegisterSavedValue<TKey, TVal>("spirefield_" + Name, base.Get, base.Set, serializer, deserializer);
	}

	private static void AddToProperties(SavedProperties props, string name, object? value)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		if (value == null)
		{
			return;
		}
		if (!(value is int num))
		{
			if (!(value is bool flag))
			{
				if (!(value is string text))
				{
					if (!(value is Enum value2))
					{
						ModelId val = (ModelId)((value is ModelId) ? value : null);
						if (val == null)
						{
							SerializableCard val2 = (SerializableCard)((value is SerializableCard) ? value : null);
							if (val2 == null)
							{
								if (!(value is int[] array))
								{
									if (!(value is Enum[] source))
									{
										if (!(value is SerializableCard[] array2))
										{
											if (value is List<SerializableCard> list)
											{
												SavedProperties val3 = props;
												(val3.cardArrays ?? (val3.cardArrays = new List<SavedProperty<SerializableCard[]>>())).Add(new SavedProperty<SerializableCard[]>(name, list.ToArray()));
											}
										}
										else
										{
											SavedProperties val3 = props;
											(val3.cardArrays ?? (val3.cardArrays = new List<SavedProperty<SerializableCard[]>>())).Add(new SavedProperty<SerializableCard[]>(name, array2));
										}
									}
									else
									{
										SavedProperties val3 = props;
										(val3.intArrays ?? (val3.intArrays = new List<SavedProperty<int[]>>())).Add(new SavedProperty<int[]>(name, source.Select(Convert.ToInt32).ToArray()));
									}
								}
								else
								{
									SavedProperties val3 = props;
									(val3.intArrays ?? (val3.intArrays = new List<SavedProperty<int[]>>())).Add(new SavedProperty<int[]>(name, array));
								}
							}
							else
							{
								SavedProperties val3 = props;
								(val3.cards ?? (val3.cards = new List<SavedProperty<SerializableCard>>())).Add(new SavedProperty<SerializableCard>(name, val2));
							}
						}
						else
						{
							SavedProperties val3 = props;
							(val3.modelIds ?? (val3.modelIds = new List<SavedProperty<ModelId>>())).Add(new SavedProperty<ModelId>(name, val));
						}
					}
					else
					{
						SavedProperties val3 = props;
						(val3.ints ?? (val3.ints = new List<SavedProperty<int>>())).Add(new SavedProperty<int>(name, Convert.ToInt32(value2)));
					}
				}
				else
				{
					SavedProperties val3 = props;
					(val3.strings ?? (val3.strings = new List<SavedProperty<string>>())).Add(new SavedProperty<string>(name, text));
				}
			}
			else
			{
				SavedProperties val3 = props;
				(val3.bools ?? (val3.bools = new List<SavedProperty<bool>>())).Add(new SavedProperty<bool>(name, flag));
			}
		}
		else
		{
			SavedProperties val3 = props;
			(val3.ints ?? (val3.ints = new List<SavedProperty<int>>())).Add(new SavedProperty<int>(name, num));
		}
	}

	private static bool TryGetFromProperties<T>(SavedProperties props, string name, out T? value)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		value = default(T);
		if (typeof(T) == typeof(int) || typeof(T).IsEnum)
		{
			SavedProperty<int>? val = props.ints?.FirstOrDefault((Func<SavedProperty<int>, bool>)((SavedProperty<int> p) => p.name == name));
			if (!val.HasValue)
			{
				return false;
			}
			value = (typeof(T).IsEnum ? ((T)Enum.ToObject(typeof(T), val.Value.value)) : ((T)(object)val.Value.value));
			return true;
		}
		if (typeof(T) == typeof(bool))
		{
			SavedProperty<bool>? val2 = props.bools?.FirstOrDefault((Func<SavedProperty<bool>, bool>)((SavedProperty<bool> p) => p.name == name));
			if (!val2.HasValue)
			{
				return false;
			}
			value = (T)(object)val2.Value.value;
			return true;
		}
		if (typeof(T) == typeof(string))
		{
			SavedProperty<string>? val3 = props.strings?.FirstOrDefault((Func<SavedProperty<string>, bool>)((SavedProperty<string> p) => p.name == name));
			if (!val3.HasValue)
			{
				return false;
			}
			value = (T)(object)val3.Value.value;
			return true;
		}
		if (typeof(T) == typeof(ModelId))
		{
			SavedProperty<ModelId>? val4 = props.modelIds?.FirstOrDefault((Func<SavedProperty<ModelId>, bool>)((SavedProperty<ModelId> p) => p.name == name));
			if (!val4.HasValue)
			{
				return false;
			}
			value = (T)(object)val4.Value.value;
			return true;
		}
		if (typeof(T) == typeof(int[]) || (typeof(T).IsArray && typeof(T).GetElementType().IsEnum))
		{
			SavedProperty<int[]>? val5 = props.intArrays?.FirstOrDefault((Func<SavedProperty<int[]>, bool>)((SavedProperty<int[]> p) => p.name == name));
			if (!val5.HasValue)
			{
				return false;
			}
			if (typeof(T).IsArray && typeof(T).GetElementType().IsEnum)
			{
				Type elementType = typeof(T).GetElementType();
				Array array = Array.CreateInstance(elementType, val5.Value.value.Length);
				for (int num = 0; num < val5.Value.value.Length; num++)
				{
					array.SetValue(Enum.ToObject(elementType, val5.Value.value[num]), num);
				}
				value = (T)(object)array;
			}
			else
			{
				value = (T)(object)val5.Value.value;
			}
			return true;
		}
		if (typeof(T) == typeof(SerializableCard))
		{
			SavedProperty<SerializableCard>? val6 = props.cards?.FirstOrDefault((Func<SavedProperty<SerializableCard>, bool>)((SavedProperty<SerializableCard> p) => p.name == name));
			if (!val6.HasValue)
			{
				return false;
			}
			value = (T)(object)val6.Value.value;
			return true;
		}
		if (typeof(T) == typeof(SerializableCard[]) || typeof(T) == typeof(List<SerializableCard>))
		{
			SavedProperty<SerializableCard[]>? val7 = props.cardArrays?.FirstOrDefault((Func<SavedProperty<SerializableCard[]>, bool>)((SavedProperty<SerializableCard[]> p) => p.name == name));
			if (!val7.HasValue)
			{
				return false;
			}
			value = ((typeof(T) == typeof(List<SerializableCard>)) ? ((T)(object)val7.Value.value.ToList()) : ((T)(object)val7.Value.value));
			return true;
		}
		return false;
	}
}
