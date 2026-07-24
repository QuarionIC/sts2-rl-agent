using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__SpecialCharRegex_0 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan))
				{
					int num = runtextpos;
					Capture(0, num, runtextpos = num + 1);
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if ((uint)num < (uint)inputSpan.Length)
				{
					int num2 = inputSpan.Slice(num).IndexOfAnyExcept(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_ascii_40FF03FEFFFF87FEFFFF07);
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__SpecialCharRegex_0 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__SpecialCharRegex_0();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__SpecialCharRegex_0()
	{
		pattern = "[^a-zA-Z0-9_.]";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
