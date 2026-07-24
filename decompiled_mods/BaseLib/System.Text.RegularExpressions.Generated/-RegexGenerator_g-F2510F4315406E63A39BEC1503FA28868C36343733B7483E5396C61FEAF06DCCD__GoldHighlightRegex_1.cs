using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__GoldHighlightRegex_1 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 2)
				{
					int num2 = inputSpan.Slice(num).IndexOf('*');
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num10 = num;
				if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num11 = num;
				if (num != 0)
				{
					num = num11;
					span = inputSpan.Slice(num);
					if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '/')
					{
						UncaptureUntil(0);
						return false;
					}
					num--;
				}
				num = num10;
				span = inputSpan.Slice(num);
				if (span.IsEmpty || span[0] != '*')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				if (span.IsEmpty || span[0] != '{' || (uint)span.Length < 2u || span[1] == '\n')
				{
					goto IL_01d2;
				}
				num += 2;
				span = inputSpan.Slice(num);
				num8 = num;
				while (true)
				{
					num6 = Crawlpos();
					if (!span.IsEmpty && span[0] == '}')
					{
						break;
					}
					UncaptureUntil(num6);
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num8;
					span = inputSpan.Slice(num);
					if (!span.IsEmpty && span[0] != '\n')
					{
						num++;
						span = inputSpan.Slice(num);
						num8 = span.IndexOfAny('\n', '}');
						if ((uint)num8 < (uint)span.Length && span[num8] != '\n')
						{
							num += num8;
							span = inputSpan.Slice(num);
							num8 = num;
							continue;
						}
					}
					goto IL_01d2;
				}
				num2 = 0;
				num++;
				span = inputSpan.Slice(num);
				goto IL_032a;
				IL_032a:
				Capture(1, num5, num);
				if (!span.IsEmpty && span[0] == '*')
				{
					span = span.Slice(1);
					num++;
				}
				runtextpos = num;
				Capture(0, start, num);
				return true;
				IL_01d2:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (span.IsEmpty || span[0] == '\n')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num9 = num;
				int num12;
				while (true)
				{
					num7 = Crawlpos();
					span = inputSpan.Slice(num);
					num12 = num;
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					int num13 = num;
					if (num >= inputSpan.Length - 1 && ((uint)num >= (uint)inputSpan.Length || inputSpan[num] == '\n'))
					{
						break;
					}
					num = num13;
					span = inputSpan.Slice(num);
					char c;
					if (span.IsEmpty || (((c = span[0]) < '\u0080') ? (("㸀\0吁\0\0\0\0\u3000"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\0\b\u0001*+,-./|~d"))))
					{
						UncaptureUntil(num7);
						if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						num = num9;
						span = inputSpan.Slice(num);
						if (span.IsEmpty || span[0] == '\n')
						{
							UncaptureUntil(0);
							return false;
						}
						num++;
						span = inputSpan.Slice(num);
						num9 = num;
						continue;
					}
					num++;
					span = inputSpan.Slice(num);
					break;
				}
				num = num12;
				span = inputSpan.Slice(num);
				num2 = 1;
				goto IL_032a;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__GoldHighlightRegex_1 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__GoldHighlightRegex_1();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__GoldHighlightRegex_1()
	{
		pattern = "(?<=^|[^/])\\*({.+?}|.+?(?=$|[\\s*.,|}]))\\*?";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
