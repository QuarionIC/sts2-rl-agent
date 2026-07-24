using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;

namespace BaseLib.Utils;

public static class CustomCharacterUtils
{
	internal static List<List<Type>>? TypesToSort { get; set; } = new List<List<Type>>();

	public static bool TryOrderCustomCharacters([ParamCollection] List<Type> characters)
	{
		if (TypesToSort == null || characters.Any((Type t) => !t.IsSubclassOf(typeof(CustomCharacterModel))))
		{
			return false;
		}
		TypesToSort.Add(characters);
		return true;
	}

	public static bool TryOrderCustomCharacters<T1, T2>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel
	{
		int num = 2;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel
	{
		int num = 3;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel
	{
		int num = 4;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel
	{
		int num = 5;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5, T6>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel where T6 : CustomCharacterModel
	{
		int num = 6;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		num2++;
		span[num2] = typeof(T6);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5, T6, T7>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel where T6 : CustomCharacterModel where T7 : CustomCharacterModel
	{
		int num = 7;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		num2++;
		span[num2] = typeof(T6);
		num2++;
		span[num2] = typeof(T7);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel where T6 : CustomCharacterModel where T7 : CustomCharacterModel where T8 : CustomCharacterModel
	{
		int num = 8;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		num2++;
		span[num2] = typeof(T6);
		num2++;
		span[num2] = typeof(T7);
		num2++;
		span[num2] = typeof(T8);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel where T6 : CustomCharacterModel where T7 : CustomCharacterModel where T8 : CustomCharacterModel where T9 : CustomCharacterModel
	{
		int num = 9;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		num2++;
		span[num2] = typeof(T6);
		num2++;
		span[num2] = typeof(T7);
		num2++;
		span[num2] = typeof(T8);
		num2++;
		span[num2] = typeof(T9);
		return TryOrderCustomCharacters(list);
	}

	public static bool TryOrderCustomCharacters<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : CustomCharacterModel where T2 : CustomCharacterModel where T3 : CustomCharacterModel where T4 : CustomCharacterModel where T5 : CustomCharacterModel where T6 : CustomCharacterModel where T7 : CustomCharacterModel where T8 : CustomCharacterModel where T9 : CustomCharacterModel where T10 : CustomCharacterModel
	{
		int num = 10;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(T1);
		num2++;
		span[num2] = typeof(T2);
		num2++;
		span[num2] = typeof(T3);
		num2++;
		span[num2] = typeof(T4);
		num2++;
		span[num2] = typeof(T5);
		num2++;
		span[num2] = typeof(T6);
		num2++;
		span[num2] = typeof(T7);
		num2++;
		span[num2] = typeof(T8);
		num2++;
		span[num2] = typeof(T9);
		num2++;
		span[num2] = typeof(T10);
		return TryOrderCustomCharacters(list);
	}
}
