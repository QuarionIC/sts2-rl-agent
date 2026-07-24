using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__DiffVariableRegex_4 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOf('!');
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '!')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				num4 = num;
				while (true)
				{
					num3 = Crawlpos();
					Capture(1, num2, num);
					if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '!')
					{
						break;
					}
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num4;
					readOnlySpan = inputSpan.Slice(num);
					if (readOnlySpan.IsEmpty || readOnlySpan[0] == '\n')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					readOnlySpan = inputSpan.Slice(num);
					num4 = readOnlySpan.IndexOfAny('\n', '!');
					if ((uint)num4 >= (uint)readOnlySpan.Length || readOnlySpan[num4] == '\n')
					{
						UncaptureUntil(0);
						return false;
					}
					num += num4;
					readOnlySpan = inputSpan.Slice(num);
					num4 = num;
				}
				Capture(0, start, runtextpos = num + 1);
				return true;
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

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__DiffVariableRegex_4 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__DiffVariableRegex_4();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__DiffVariableRegex_4()
	{
		pattern = "!(.*?)!";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
