using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__PluralizeRegex_7 : Regex
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
				if (num <= inputSpan.Length - 6)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					for (int i = 0; i < readOnlySpan.Length; i++)
					{
						if (readOnlySpan[i] != '\n')
						{
							runtextpos = num + i;
							return true;
						}
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
				int arg = 0;
				int arg2 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int num15 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num5 = num;
				num12 = num;
				while (true)
				{
					num9 = Crawlpos();
					if (!span.IsEmpty && span[0] == '{')
					{
						num++;
						span = inputSpan.Slice(num);
						Capture(1, num5, num);
						num6 = num;
						if (!span.IsEmpty && span[0] != '{')
						{
							num++;
							span = inputSpan.Slice(num);
							num13 = num;
							while (true)
							{
								num10 = Crawlpos();
								Capture(2, num6, num);
								num7 = num;
								num15 = 0;
								while (true)
								{
									_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
									num15++;
									if (!span.IsEmpty && span[0] == ':')
									{
										num++;
										span = inputSpan.Slice(num);
										arg = num;
										int num16 = span.IndexOf('{');
										if (num16 < 0)
										{
											num16 = span.Length;
										}
										span = span.Slice(num16);
										num += num16;
										arg2 = num;
										goto IL_0274;
									}
									goto IL_0294;
									IL_0274:
									_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
									if (num15 == 0)
									{
										continue;
									}
									goto IL_02e9;
									IL_0294:
									if (--num15 < 0)
									{
										break;
									}
									num = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									span = inputSpan.Slice(num);
									goto IL_02e9;
									IL_02e9:
									if (!span.IsEmpty && span[0] == '}')
									{
										num4 = num;
										num3 = Crawlpos();
										num++;
										span = inputSpan.Slice(num);
										num14 = num;
										while (true)
										{
											num11 = Crawlpos();
											char c;
											if (span.IsEmpty || (c = span[0]) == '/' || c == '{')
											{
												goto IL_031d;
											}
											num2 = 0;
											num++;
											span = inputSpan.Slice(num);
											goto IL_03e1;
											IL_031d:
											UncaptureUntil(num11);
											if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											num = num14;
											span = inputSpan.Slice(num);
											if (!span.IsEmpty && span[0] != '{')
											{
												num++;
												span = inputSpan.Slice(num);
												num14 = num;
												continue;
											}
											num = num4;
											span = inputSpan.Slice(num);
											UncaptureUntil(num3);
											num2 = 1;
											num++;
											span = inputSpan.Slice(num);
											goto IL_03e1;
											IL_03e1:
											while (true)
											{
												Capture(3, num7, num);
												if (!span.IsEmpty && span[0] == '(')
												{
													num++;
													span = inputSpan.Slice(num);
													num8 = num;
													int num17 = span.IndexOfAny('(', ')');
													if (num17 < 0)
													{
														num17 = span.Length;
													}
													if (num17 != 0)
													{
														span = span.Slice(num17);
														num += num17;
														Capture(4, num8, num);
														if (!span.IsEmpty && span[0] == ')')
														{
															Capture(0, start, runtextpos = num + 1);
															return true;
														}
													}
												}
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
													goto end_IL_0366;
												}
											}
											goto IL_031d;
											continue;
											end_IL_0366:
											break;
										}
									}
									if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (num15 != 0)
									{
										UncaptureUntil(runstack[--pos]);
										_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
										if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
										{
											CheckTimeout();
										}
										if (arg < arg2)
										{
											num = --arg2;
											span = inputSpan.Slice(num);
											goto IL_0274;
										}
										goto IL_0294;
									}
									break;
								}
								UncaptureUntil(num10);
								if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								num = num13;
								span = inputSpan.Slice(num);
								if (span.IsEmpty || span[0] == '{')
								{
									break;
								}
								num++;
								span = inputSpan.Slice(num);
								num13 = num;
							}
						}
					}
					UncaptureUntil(num9);
					if (_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num12;
					span = inputSpan.Slice(num);
					if (span.IsEmpty || span[0] == '\n')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					span = inputSpan.Slice(num);
					num12 = span.IndexOfAny('\n', '{');
					if ((uint)num12 >= (uint)span.Length || span[num12] == '\n')
					{
						break;
					}
					num += num12;
					span = inputSpan.Slice(num);
					num12 = num;
				}
				UncaptureUntil(0);
				return false;
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

	internal static readonly _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__PluralizeRegex_7 Instance = new _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__PluralizeRegex_7();

	private _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__PluralizeRegex_7()
	{
		pattern = "(.*?{)([^{]+?)((?::[^{]*)?}(?:(?:[^{]*?[^{/])|(?:)))\\(([^()]+?)\\)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 5;
	}
}
