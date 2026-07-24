using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__EnergyIconsRegex_8 : Regex
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
				if (num <= inputSpan.Length - 3)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_indexOfString_665CDE8E8271C813BEFD18996267DFB1EA73747FE0F0B4121CA25DF119118C4D);
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (span.IsEmpty || span[0] != '[')
				{
					UncaptureUntil(0);
					return false;
				}
				num4 = num;
				num3 = Crawlpos();
				num++;
				span = inputSpan.Slice(num);
				num5 = num;
				if (!span.StartsWith("E?".AsSpan()))
				{
					goto IL_008c;
				}
				num += 2;
				span = inputSpan.Slice(num);
				Capture(1, num5, num);
				num2 = 0;
				goto IL_0112;
				IL_0112:
				while (true)
				{
					if (span.IsEmpty || span[0] != ']')
					{
						if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num2 == 0)
						{
							break;
						}
						if (num2 == 1)
						{
							UncaptureUntil(0);
							return false;
						}
						continue;
					}
					Capture(0, start, runtextpos = num + 1);
					return true;
				}
				goto IL_008c;
				IL_008c:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				num++;
				span = inputSpan.Slice(num);
				num6 = num;
				int num7 = span.IndexOfAnyExcept('E');
				if (num7 < 0)
				{
					num7 = span.Length;
				}
				if (num7 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num7);
				num += num7;
				Capture(2, num6, num);
				num2 = 1;
				goto IL_0112;
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

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__EnergyIconsRegex_8 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__EnergyIconsRegex_8();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__EnergyIconsRegex_8()
	{
		pattern = "\\[(?:(E\\?)|(E+))\\]";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}
