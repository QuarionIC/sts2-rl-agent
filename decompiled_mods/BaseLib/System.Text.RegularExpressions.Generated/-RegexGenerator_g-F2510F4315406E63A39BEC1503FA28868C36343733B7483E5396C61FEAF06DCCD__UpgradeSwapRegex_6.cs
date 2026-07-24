using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__UpgradeSwapRegex_6 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOfAny('+', '-');
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
				int start2 = 0;
				int start3 = 0;
				int capturePosition = 0;
				int capturePosition2 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int pos = 0;
				int num10 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num11 = num;
				if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num12 = num;
				if (num != 0)
				{
					num = num12;
					readOnlySpan = inputSpan.Slice(num);
					if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '/')
					{
						UncaptureUntil(0);
						return false;
					}
					num--;
				}
				num = num11;
				readOnlySpan = inputSpan.Slice(num);
				num4 = num;
				num3 = Crawlpos();
				if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '-')
				{
					num++;
					readOnlySpan = inputSpan.Slice(num);
					start2 = num;
					if (!readOnlySpan.IsEmpty && readOnlySpan[0] != '\n')
					{
						num++;
						readOnlySpan = inputSpan.Slice(num);
						num6 = num;
						goto IL_018c;
					}
				}
				goto IL_01cf;
				IL_018c:
				capturePosition = Crawlpos();
				Capture(1, start2, num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '-')
				{
					goto IL_0108;
				}
				num2 = 0;
				num++;
				readOnlySpan = inputSpan.Slice(num);
				goto IL_02e8;
				IL_02e8:
				while (true)
				{
					int num13 = pos;
					num10 = pos;
					num9 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num9++;
						if (readOnlySpan.IsEmpty || readOnlySpan[0] != '+')
						{
							goto IL_043a;
						}
						num++;
						readOnlySpan = inputSpan.Slice(num);
						int num14 = num;
						num8 = num;
						while (true)
						{
							num5 = Crawlpos();
							_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, num8, num5);
							if (!readOnlySpan.IsEmpty && readOnlySpan[0] != '/')
							{
								num++;
								readOnlySpan = inputSpan.Slice(num);
								Capture(3, num14, num);
								_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, num14);
								if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '+')
								{
									break;
								}
								num14 = runstack[--pos];
							}
							_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPop(runstack, ref pos, out num5, out num8);
							UncaptureUntil(num5);
							if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							num = num8;
							readOnlySpan = inputSpan.Slice(num);
							if (!readOnlySpan.IsEmpty && readOnlySpan[0] != '\n')
							{
								num++;
								readOnlySpan = inputSpan.Slice(num);
								num8 = num;
								continue;
							}
							goto IL_043a;
						}
						num++;
						readOnlySpan = inputSpan.Slice(num);
						if (num9 == 0)
						{
							continue;
						}
						goto IL_0474;
						IL_043a:
						if (--num9 < 0)
						{
							break;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0474;
						IL_0474:
						pos = num10;
						pos = num13;
						runtextpos = num;
						Capture(0, start, num);
						return true;
					}
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 == 0)
					{
						break;
					}
					if (num2 != 1)
					{
						continue;
					}
					goto IL_0218;
				}
				goto IL_0108;
				IL_026a:
				capturePosition2 = Crawlpos();
				if (!readOnlySpan.IsEmpty && readOnlySpan[0] != '/')
				{
					num++;
					readOnlySpan = inputSpan.Slice(num);
					Capture(2, start3, num);
					if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '+')
					{
						num2 = 1;
						num++;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_02e8;
					}
				}
				goto IL_0218;
				IL_0108:
				UncaptureUntil(capturePosition);
				if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				num = num6;
				readOnlySpan = inputSpan.Slice(num);
				if (!readOnlySpan.IsEmpty && readOnlySpan[0] != '\n')
				{
					num++;
					readOnlySpan = inputSpan.Slice(num);
					num6 = readOnlySpan.IndexOfAny('\n', '-');
					if ((uint)num6 < (uint)readOnlySpan.Length && readOnlySpan[num6] != '\n')
					{
						num += num6;
						readOnlySpan = inputSpan.Slice(num);
						num6 = num;
						goto IL_018c;
					}
				}
				goto IL_01cf;
				IL_0218:
				UncaptureUntil(capturePosition2);
				if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				num = num7;
				readOnlySpan = inputSpan.Slice(num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] == '\n')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num7 = num;
				goto IL_026a;
				IL_01cf:
				num = num4;
				readOnlySpan = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '+')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				start3 = num;
				num7 = num;
				goto IL_026a;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num15)
				{
					while (Crawlpos() > num15)
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

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__UpgradeSwapRegex_6 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__UpgradeSwapRegex_6();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__UpgradeSwapRegex_6()
	{
		pattern = "(?<=^|[^/])(?:(?:-(.+?)-)|(?:\\+(.*?[^/])\\+))(?:\\+(.*?[^/])\\+)?";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 4;
	}
}
