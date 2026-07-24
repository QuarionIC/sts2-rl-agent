using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__LogPrefixRegex_9 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 4 && num == 0)
				{
					return true;
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
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int num15 = 0;
				int num16 = 0;
				int num17 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num6 = num;
				num4 = Crawlpos();
				if (!span.IsEmpty && span[0] == '[')
				{
					num++;
					span = inputSpan.Slice(num);
					num8 = num;
					if (!span.IsEmpty)
					{
						char c = span[0];
						if ((uint)c <= 87u)
						{
							if ((uint)c <= 73u)
							{
								if (c == 'D')
								{
									goto IL_01a4;
								}
								if (c == 'E')
								{
									goto IL_0252;
								}
								if (c == 'I')
								{
									goto IL_01e1;
								}
							}
							else
							{
								if (c == 'L')
								{
									goto IL_0167;
								}
								if (c == 'V')
								{
									goto IL_0128;
								}
								if (c == 'W')
								{
									goto IL_021b;
								}
							}
						}
						else if ((uint)c <= 105u)
						{
							if (c == 'd')
							{
								goto IL_01a4;
							}
							if (c == 'e')
							{
								goto IL_0252;
							}
							if (c == 'i')
							{
								goto IL_01e1;
							}
						}
						else
						{
							if (c == 'l')
							{
								goto IL_0167;
							}
							if (c == 'v')
							{
								goto IL_0128;
							}
							if (c == 'w')
							{
								goto IL_021b;
							}
						}
					}
				}
				goto IL_02b9;
				IL_04bd:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length < 5u || !span.StartsWith("error".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num3 = 5;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_0541;
				IL_0588:
				num15 = num;
				int i;
				for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				num16 = num;
				num12 = Crawlpos();
				num10 = num;
				int num18 = inputSpan.Length - num;
				span = span.Slice(num18);
				num += num18;
				Capture(2, num10, num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
				IL_0252:
				if ((uint)span.Length >= 5u && span.Slice(1).StartsWith("rror".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 5;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_021b:
				if ((uint)span.Length >= 4u && span.Slice(1).StartsWith("arn".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 4;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_0128:
				if ((uint)span.Length >= 9u && span.Slice(1).StartsWith("erydebug".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 9;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_02b9:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				num9 = num;
				num7 = num;
				num5 = Crawlpos();
				if ((uint)span.Length < 9u || !span.StartsWith("verydebug".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0311;
				}
				num3 = 0;
				num += 9;
				span = inputSpan.Slice(num);
				goto IL_0541;
				IL_0284:
				Capture(1, num8, num);
				if (span.IsEmpty || span[0] != ']')
				{
					goto IL_02b9;
				}
				num2 = 0;
				num++;
				span = inputSpan.Slice(num);
				goto IL_0588;
				IL_04b6:
				num3 = 4;
				goto IL_0541;
				IL_0359:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length < 5u || !span.StartsWith("debug".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					goto IL_03a1;
				}
				num3 = 2;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_0541;
				IL_03e9:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length >= 4u && span.StartsWith("warn".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 4;
					span = inputSpan.Slice(num);
					num17 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num17++;
						if ((uint)span.Length < 3u || !span.StartsWith("ing".AsSpan(), StringComparison.OrdinalIgnoreCase))
						{
							break;
						}
						num += 3;
						span = inputSpan.Slice(num);
						if (num17 == 0)
						{
							continue;
						}
						goto IL_04b6;
					}
					goto IL_047f;
				}
				goto IL_04bd;
				IL_01e1:
				if ((uint)span.Length >= 4u && span.Slice(1).StartsWith("nfo".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 4;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_0541:
				while (true)
				{
					Capture(1, num9, num);
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.IsBoundary(inputSpan, num))
					{
						break;
					}
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					switch (num3)
					{
					case 0:
						break;
					case 1:
						goto IL_0359;
					case 2:
						goto IL_03a1;
					case 3:
						goto IL_03e9;
					case 4:
						goto IL_047f;
					case 5:
						UncaptureUntil(0);
						return false;
					default:
						continue;
					}
					goto IL_0311;
				}
				num13 = num;
				if (!span.IsEmpty && span[0] == ':')
				{
					span = span.Slice(1);
					num++;
				}
				num14 = num;
				num11 = Crawlpos();
				num2 = 1;
				goto IL_0588;
				IL_01a4:
				if ((uint)span.Length >= 5u && span.Slice(1).StartsWith("ebug".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 5;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_0167:
				if ((uint)span.Length >= 4u && span.Slice(1).StartsWith("oad".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					num += 4;
					span = inputSpan.Slice(num);
					goto IL_0284;
				}
				goto IL_02b9;
				IL_0311:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length < 4u || !span.StartsWith("load".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0359;
				}
				num3 = 1;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_0541;
				IL_03a1:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length < 4u || !span.StartsWith("info".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					goto IL_03e9;
				}
				num3 = 3;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_0541;
				IL_047f:
				if (--num17 >= 0)
				{
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_04b6;
				}
				goto IL_04bd;
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

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__LogPrefixRegex_9 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__LogPrefixRegex_9();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__LogPrefixRegex_9()
	{
		pattern = "^(?:\\[(?<level>VERYDEBUG|LOAD|DEBUG|INFO|WARN|ERROR)\\]|(?<level>VERYDEBUG|LOAD|DEBUG|INFO|WARN(?:ING)?|ERROR)\\b:?)\\s*(?<msg>.*)";
		roptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "level", 1 },
			{ "msg", 2 }
		};
		capslist = new string[3] { "0", "level", "msg" };
		capsize = 3;
	}
}
