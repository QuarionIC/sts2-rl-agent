using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using BaseLib.Extensions;

namespace BaseLib.Patches.Content;

public static class CustomEnums
{
	private class KeyGenerator
	{
		private static readonly Dictionary<Type, Func<object, object>> Incrementers = new Dictionary<Type, Func<object, object>>
		{
			{
				typeof(byte),
				(object val) => (byte)val + 1
			},
			{
				typeof(sbyte),
				(object val) => (sbyte)val + 1
			},
			{
				typeof(short),
				(object val) => (short)val + 1
			},
			{
				typeof(ushort),
				(object val) => (ushort)val + 1
			},
			{
				typeof(int),
				(object val) => (int)val + 1
			},
			{
				typeof(uint),
				(object val) => (uint)val + 1
			},
			{
				typeof(long),
				(object val) => (long)val + 1
			},
			{
				typeof(ulong),
				(object val) => (ulong)val + 1
			}
		};

		private static readonly Dictionary<Type, Func<object, object>> FlagIncrementers = new Dictionary<Type, Func<object, object>>
		{
			{
				typeof(byte),
				FlagIncrementer<byte>()
			},
			{
				typeof(sbyte),
				FlagIncrementer<sbyte>()
			},
			{
				typeof(short),
				FlagIncrementer<short>()
			},
			{
				typeof(ushort),
				FlagIncrementer<ushort>()
			},
			{
				typeof(int),
				FlagIncrementer<int>()
			},
			{
				typeof(uint),
				FlagIncrementer<uint>()
			},
			{
				typeof(long),
				FlagIncrementer<long>()
			},
			{
				typeof(ulong),
				FlagIncrementer<ulong>()
			}
		};

		private static readonly Dictionary<Type, int> TypeHalfSizes = new Dictionary<Type, int>
		{
			{
				typeof(byte),
				4
			},
			{
				typeof(sbyte),
				4
			},
			{
				typeof(short),
				8
			},
			{
				typeof(ushort),
				8
			},
			{
				typeof(int),
				16
			},
			{
				typeof(uint),
				16
			},
			{
				typeof(long),
				32
			},
			{
				typeof(ulong),
				32
			}
		};

		private Type _underlyingType;

		private object _nextKey;

		private bool _isFlag;

		private int _halfBits;

		private readonly Func<object, object> _increment;

		private HashSet<object> _values = new HashSet<object>();

		private static Func<object, object> FlagIncrementer<T>() where T : struct, IBinaryInteger<T>
		{
			return delegate(object val)
			{
				T val2 = (T)val;
				T one;
				for (one = T.One; one <= val2 && one != T.Zero; one <<= 1)
				{
				}
				return one;
			};
		}

		public KeyGenerator(Type t)
		{
			if (!t.IsEnum)
			{
				_increment = (object o) => o;
				throw new ArgumentException("Attempted to construct KeyGenerator with non-enum type " + t.FullName);
			}
			_isFlag = t.GetCustomAttribute<FlagsAttribute>() != null;
			Array enumValuesAsUnderlyingType = t.GetEnumValuesAsUnderlyingType();
			_underlyingType = Enum.GetUnderlyingType(t);
			_nextKey = Convert.ChangeType(0, _underlyingType);
			_increment = (_isFlag ? FlagIncrementers[_underlyingType] : Incrementers[_underlyingType]);
			_halfBits = TypeHalfSizes[_underlyingType];
			if (enumValuesAsUnderlyingType.Length > 0)
			{
				foreach (object item in enumValuesAsUnderlyingType)
				{
					_values.Add(item);
					if (((IComparable)item).CompareTo(_nextKey) >= 0)
					{
						_nextKey = _increment(item);
					}
				}
			}
			BaseLibMain.Logger.Info($"Generated KeyGenerator for enum {t.FullName} with starting value {_nextKey} | IsFlag: {_isFlag} | Half-Size: {_halfBits}", 1);
		}

		public object GetKey(int namespaceHash, int nameHash)
		{
			if (_isFlag)
			{
				object nextKey = _nextKey;
				_nextKey = _increment(_nextKey);
				return nextKey;
			}
			int num = namespaceHash & ((1 << _halfBits) - 1);
			int num2 = nameHash & ((1 << _halfBits) - 1);
			int num3 = (num << _halfBits) | num2;
			_nextKey = Convert.ChangeType(num3, _underlyingType);
			while (_values.Contains(_nextKey))
			{
				_nextKey = _increment(_nextKey);
			}
			_values.Add(_nextKey);
			return _nextKey;
		}
	}

	private static readonly Dictionary<Type, KeyGenerator> KeyGenerators = new Dictionary<Type, KeyGenerator>();

	public static readonly Dictionary<Type, Dictionary<int, (string Prefix, string Name)>> GeneratedCustomEnumEntries = new Dictionary<Type, Dictionary<int, (string, string)>>();

	public static string? EnumName<EnumType>(int enumVal) where EnumType : Enum
	{
		return GeneratedCustomEnumEntries.GetValueOrDefault(typeof(EnumType))?.GetValueOrDefault(enumVal).Item2;
	}

	public static T GenerateKey<T>(string @namespace, string name) where T : Enum
	{
		return (T)GenerateKey(typeof(T), @namespace, name);
	}

	public static object GenerateKey(FieldInfo field)
	{
		return GenerateKey(field.FieldType, field.DeclaringType.GetRootNamespace(), field.Name);
	}

	public static object GenerateKey(Type enumType, string @namespace, string name)
	{
		if (!KeyGenerators.TryGetValue(enumType, out KeyGenerator value))
		{
			KeyGenerators.Add(enumType, value = new KeyGenerator(enumType));
		}
		return value.GetKey(@namespace.ComputeBasicHash(), name.ComputeBasicHash());
	}
}
