using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
internal static class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	internal static readonly SearchValues<char> s_ascii_40FF03FEFFFF87FEFFFF07 = SearchValues.Create(".0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".AsSpan());

	internal static readonly SearchValues<string> s_indexOfString_665CDE8E8271C813BEFD18996267DFB1EA73747FE0F0B4121CA25DF119118C4D;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		return ((uint)num < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[num])) != ((uint)index < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[index]));
		static bool IsBoundaryWordChar(char ch)
		{
			if (!IsWordChar(ch))
			{
				return ch == '\u200c' || ch == '\u200d';
			}
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsWordChar(char ch)
	{
		ReadOnlySpan<byte> readOnlySpan = new byte[16]
		{
			0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
			255, 135, 254, 255, 255, 7
		};
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)readOnlySpan.Length)
		{
			return (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		}
		return (readOnlySpan[num] & (1 << (ch & 7))) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPop(int[] stack, ref int pos, out int arg0, out int arg1)
	{
		arg0 = stack[--pos];
		arg1 = stack[--pos];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)num < (uint)array.Length)
		{
			array[num] = arg0;
			pos++;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg1)
		{
			Array.Resize(ref reference, reference2 * 2);
			StackPush(ref reference, ref reference2, arg1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 1) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			pos += 2;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg2, int arg3)
		{
			Array.Resize(ref reference, (reference2 + 1) * 2);
			StackPush(ref reference, ref reference2, arg2, arg3);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 2) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			array[num + 2] = arg2;
			pos += 3;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1, arg2);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg3, int arg4, int arg5)
		{
			Array.Resize(ref reference, (reference2 + 2) * 2);
			StackPush(ref reference, ref reference2, arg3, arg4, arg5);
		}
	}

	static _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities()
	{
		string reference = "[E";
		s_indexOfString_665CDE8E8271C813BEFD18996267DFB1EA73747FE0F0B4121CA25DF119118C4D = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
	}
}
