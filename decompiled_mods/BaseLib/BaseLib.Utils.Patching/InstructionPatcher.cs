using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BaseLib.Extensions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace BaseLib.Utils.Patching;

public class InstructionPatcher
{
	private readonly List<CodeInstruction> _code = instructions.ToList();

	private int _index = -1;

	private int _lastMatchStart = -1;

	public readonly List<string> Log = new List<string>();

	public int Index => _index;

	public InstructionPatcher(IEnumerable<CodeInstruction> instructions)
	{
	}

	public static implicit operator List<CodeInstruction>(InstructionPatcher locator)
	{
		return locator._code;
	}

	public InstructionPatcher ResetPosition()
	{
		_index = -1;
		_lastMatchStart = -1;
		return this;
	}

	public InstructionPatcher Match(params IMatcher[] matchers)
	{
		return Match(DefaultMatchFailure, matchers);
	}

	public InstructionPatcher Match(Action<IMatcher[]> onFailMatch, params IMatcher[] matchers)
	{
		if (_index < 0)
		{
			_index = 0;
		}
		for (int i = 0; i < matchers.Length; i++)
		{
			if (!matchers[i].Match(Log, _code, _index, out _lastMatchStart, out _index))
			{
				onFailMatch(matchers);
				return this;
			}
		}
		Log.Add("Found end of match at " + _index + "; last match starts at " + _lastMatchStart);
		return this;
	}

	public InstructionPatcher? TryMatch(params IMatcher[] matchers)
	{
		if (_index < 0)
		{
			_index = 0;
		}
		for (int i = 0; i < matchers.Length; i++)
		{
			if (!matchers[i].Match(Log, _code, _index, out _lastMatchStart, out _index))
			{
				Log.Add("TryMatch failed");
				return null;
			}
		}
		Log.Add("Found end of match at " + _index + "; last match starts at " + _lastMatchStart);
		return this;
	}

	public InstructionPatcher MatchFromEnd(params IMatcher[] matchers)
	{
		return MatchFromEnd(DefaultMatchFailure, matchers);
	}

	public InstructionPatcher MatchFromEnd(Action<IMatcher[]> onFailMatch, params IMatcher[] matchers)
	{
		int num = _code.Count;
		while (num > 0)
		{
			num = (_index = num - 1);
			bool flag = true;
			for (int i = 0; i < matchers.Length; i++)
			{
				if (!matchers[i].Match(Log, _code, _index, out _lastMatchStart, out _index))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (num == 0)
		{
			onFailMatch(matchers);
			return this;
		}
		Log.Add("Found end of match at " + _index + "; last match starts at " + _lastMatchStart);
		return this;
	}

	public InstructionPatcher MatchStart()
	{
		_index = 0;
		_lastMatchStart = 0;
		return this;
	}

	public InstructionPatcher MatchEnd()
	{
		_index = _code.Count;
		_lastMatchStart = 0;
		return this;
	}

	public InstructionPatcher Step(int amt = 1)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Step without any match found");
		}
		_index += amt;
		Log.Add("Stepped to " + _index);
		return this;
	}

	public InstructionPatcher AddLabel(ILGenerator generator, out Label label)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to AddLabel without any match found");
		}
		label = generator.DefineLabel();
		CodeInstructionExtensions.WithLabels(_code[_index], new Label[1] { label });
		return this;
	}

	public InstructionPatcher GetLabels(out List<Label> labels)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetLabels without any match found");
		}
		labels = _code[_index].labels;
		if (labels.Count == 0)
		{
			if (_code[_index].operand is Label)
			{
				BaseLibMain.Logger.Info($"Code instruction {_code[_index]} has no labels. Did you mean to use GetOperandLabel instead?", 1);
			}
			else
			{
				BaseLibMain.Logger.Info($"Code instruction {_code[_index]} has no labels", 1);
			}
		}
		return this;
	}

	public InstructionPatcher TakeLabels(out List<Label> labels)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetLabels without any match found");
		}
		labels = CodeInstructionExtensions.ExtractLabels(_code[_index]);
		if (labels.Count == 0)
		{
			if (_code[_index].operand is Label)
			{
				BaseLibMain.Logger.Info($"Code instruction {_code[_index]} has no labels. Did you mean to use GetOperandLabel instead?", 1);
			}
			else
			{
				BaseLibMain.Logger.Info($"Code instruction {_code[_index]} has no labels", 1);
			}
		}
		return this;
	}

	public InstructionPatcher GetOperandLabel(out Label label)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetOperandLabel without any match found");
		}
		if (_code[_index].operand is Label label2)
		{
			label = label2;
			return this;
		}
		throw new InvalidOperationException("Code instruction " + ((object)_code[_index]).ToString() + " does not have a Label parameter");
	}

	public InstructionPatcher GetInstruction(out CodeInstruction instruction)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetInstruction without any match found");
		}
		instruction = _code[_index];
		Log.Add($"Got instruction [{instruction}]");
		return this;
	}

	public InstructionPatcher GetOperand(out object operand)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetOperand without any match found");
		}
		operand = _code[_index].operand;
		Log.Add($"Got operand [{operand?.GetType().FullName}]{operand}");
		return this;
	}

	public InstructionPatcher GetIndexOperand(out int operand)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to GetOperand without any match found");
		}
		CodeInstruction val = _code[_index];
		switch (val.opcode.Value)
		{
		case 2:
		case 6:
		case 10:
			operand = 0;
			break;
		case 3:
		case 7:
		case 11:
			operand = 1;
			break;
		case 4:
		case 8:
		case 12:
			operand = 2;
			break;
		case 5:
		case 9:
		case 13:
			operand = 3;
			break;
		case 14:
		case 17:
		case 19:
		case 65033:
		case 65036:
		case 65038:
			if (val.operand is LocalBuilder localBuilder)
			{
				operand = localBuilder.LocalIndex;
			}
			else
			{
				operand = Convert.ToInt32(val.operand);
			}
			break;
		default:
			throw new InvalidOperationException($"Unsupported opcode for GetIndexOperand: {val.opcode}");
		}
		return this;
	}

	public InstructionPatcher TryGetIntValue(out int? val)
	{
		val = null;
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to TryGetIntValue without any match found");
		}
		if (_code[_index].TryGetIntValue(out var result))
		{
			val = result;
		}
		return this;
	}

	public InstructionPatcher ReplaceLastMatch(IEnumerable<CodeInstruction> replacement)
	{
		if (_lastMatchStart < 0)
		{
			throw new InvalidOperationException("Attempted to ReplaceLastMatch without any match found");
		}
		int num = 0;
		foreach (CodeInstruction item in replacement)
		{
			int num2 = _lastMatchStart + num;
			if (num2 > _index)
			{
				_index = num2;
				_code.Insert(_index, item);
			}
			else
			{
				_code[_lastMatchStart + num] = item;
			}
			num++;
		}
		if (_lastMatchStart + num < _index)
		{
			_code.RemoveRange(_lastMatchStart + num, _index - (_lastMatchStart + num));
			_index = _lastMatchStart + num;
		}
		else
		{
			_index++;
		}
		return this;
	}

	public InstructionPatcher Replace(CodeInstruction replacement, bool keepLabels = true)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Replace without any match found");
		}
		if (keepLabels)
		{
			CodeInstructionExtensions.MoveLabelsFrom(replacement, _code[_index]);
		}
		Log.Add($"{_code[_index]} => {replacement}");
		_code[_index] = replacement;
		return this;
	}

	public InstructionPatcher IncrementIntPush()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected O, but got Unknown
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Expected O, but got Unknown
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Replace without any match found");
		}
		switch (_code[_index].opcode.Value)
		{
		case 21:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_0, (object)null));
		case 22:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_1, (object)null));
		case 23:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_2, (object)null));
		case 24:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_3, (object)null));
		case 25:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_4, (object)null));
		case 26:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_5, (object)null));
		case 27:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_6, (object)null));
		case 28:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_7, (object)null));
		case 29:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_8, (object)null));
		case 30:
			return Replace(new CodeInstruction(OpCodes.Ldc_I4_S, (object)(sbyte)9));
		case 31:
		{
			if (_code[_index].TryGetIntValue(out var result2))
			{
				result2++;
				if (result2 > 127)
				{
					return Replace(new CodeInstruction(OpCodes.Ldc_I4, (object)result2));
				}
				return Replace(new CodeInstruction(OpCodes.Ldc_I4_S, (object)(sbyte)result2));
			}
			throw new InvalidOperationException("Failed to determine integer value of " + ((object)_code[_index])?.ToString() + " to incremented");
		}
		case 32:
		{
			if (_code[_index].TryGetIntValue(out var result))
			{
				return Replace(new CodeInstruction(OpCodes.Ldc_I4, (object)(result + 1)));
			}
			throw new InvalidOperationException("Failed to determine integer value of " + ((object)_code[_index])?.ToString() + " to incremented");
		}
		default:
			throw new InvalidOperationException("Instruction " + ((object)_code[_index])?.ToString() + " is not an int push instruction that can be incremented");
		}
	}

	public InstructionPatcher IncrementIntPush(out CodeInstruction replacedPush)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Replace without any match found");
		}
		replacedPush = _code[_index];
		return IncrementIntPush();
	}

	public InstructionPatcher Insert(CodeInstruction instruction)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Insert without any match found");
		}
		_code.Insert(_index, instruction);
		_index++;
		Log.Add($"Inserted 1 instruction, new index {_index}");
		return this;
	}

	public InstructionPatcher Insert(IEnumerable<CodeInstruction> insert)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to Insert without any match found");
		}
		CodeInstruction[] array = (insert as CodeInstruction[]) ?? insert.ToArray();
		_code.InsertRange(_index, array);
		_index += array.Length;
		Log.Add($"Inserted {array.Length} instructions, new index {_index}");
		return this;
	}

	public InstructionPatcher InsertBeforeMatch(IEnumerable<CodeInstruction> insert)
	{
		if (_index < 0 || _lastMatchStart < 0)
		{
			throw new InvalidOperationException("Attempted to Insert without any match found");
		}
		_index = _lastMatchStart;
		_lastMatchStart = -1;
		CodeInstruction[] array = (insert as CodeInstruction[]) ?? insert.ToArray();
		_code.InsertRange(_index, array);
		_index += array.Length;
		Log.Add($"Inserted {array.Length} instructions, new index {_index}");
		return this;
	}

	public InstructionPatcher CopyMatch(out List<CodeInstruction> match)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to CopyMatch without any match found");
		}
		match = (from instruction in _code.GetRange(_lastMatchStart, _index - _lastMatchStart)
			select instruction.Clone()).ToList();
		Log.Add($"Copied {match.Count} instructions:\n");
		foreach (CodeInstruction item in match)
		{
			Log.Add($" - {item}");
		}
		return this;
	}

	public InstructionPatcher InsertCopy(int startOffset, int copyLength)
	{
		if (_index < 0)
		{
			throw new InvalidOperationException("Attempted to InsertCopy without any match found");
		}
		int num = _index + startOffset;
		if (num < 0)
		{
			throw new InvalidOperationException($"startIndex of InsertCopy less than 0 ({num})");
		}
		List<CodeInstruction> list = new List<CodeInstruction>();
		for (int i = 0; i < copyLength; i++)
		{
			Log.Add("Inserting Copy: " + (object)_code[num + i]);
			list.Add(_code[num + i].Clone());
		}
		return Insert((IEnumerable<CodeInstruction>)list);
	}

	public InstructionPatcher PrintLog(Logger logger)
	{
		logger.Info(Log.AsReadable("\n"), 1);
		return this;
	}

	public InstructionPatcher PrintResult(Logger logger)
	{
		logger.Info("----- RESULT -----\n" + ((List<CodeInstruction>)this).NumberedLines(), 1);
		return this;
	}

	private void DefaultMatchFailure(IMatcher[] matchers)
	{
		throw new Exception("Failed to find match:\n" + matchers.AsReadable("\n---------\n") + "\nLOG:\n" + Log.AsReadable("\n"));
	}
}
