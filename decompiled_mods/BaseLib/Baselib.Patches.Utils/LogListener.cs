using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using BaseLib.BaseLibScenes;
using BaseLib.Config;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;

namespace BaseLib.Patches.Utils;

public class LogListener : Logger
{
	public class MethodName : MethodName
	{
		public static readonly StringName _LogMessage = StringName.op_Implicit("_LogMessage");

		public static readonly StringName _LogError = StringName.op_Implicit("_LogError");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	[GeneratedRegex("^(?:\\[(?<level>VERYDEBUG|LOAD|DEBUG|INFO|WARN|ERROR)\\]|(?<level>VERYDEBUG|LOAD|DEBUG|INFO|WARN(?:ING)?|ERROR)\\b:?)\\s*(?<msg>.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex LogPrefixRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__LogPrefixRegex_9.Instance;

	public override void _LogMessage(string message, bool error)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		Match match = LogPrefixRegex.Match(message);
		string text;
		string text2;
		if (error)
		{
			text = "ERROR";
			text2 = (match.Success ? match.Groups["msg"].Value : message);
		}
		else if (match.Success)
		{
			text = match.Groups["level"].Value.ToUpperInvariant();
			if (text == "WARNING")
			{
				text = "WARN";
			}
			text2 = match.Groups["msg"].Value;
		}
		else
		{
			text = "INFO";
			text2 = message;
		}
		NLogWindow.AddLog("[" + text + "] " + text2);
		if (text == "ERROR" && BaseLibConfig.OpenLogWindowOnError)
		{
			Callable val = Callable.From((Action)NLogWindow.OpenOnErr);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Array<ScriptBacktrace> scriptBacktraces)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		string value = ((object)(ErrorType)errorType/*cast due to .constrained prefix*/).ToString();
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(36, 6, stringBuilder);
		handler.AppendLiteral("[ERROR] Error occurred [");
		handler.AppendFormatted(value);
		handler.AppendLiteral("]: ");
		handler.AppendFormatted(rationale);
		handler.AppendLiteral("\n");
		handler.AppendFormatted(code);
		handler.AppendLiteral("\n");
		handler.AppendFormatted(file);
		handler.AppendLiteral(":");
		handler.AppendFormatted(line);
		handler.AppendLiteral(" @ ");
		handler.AppendFormatted(function);
		handler.AppendLiteral("()\n");
		StringBuilder stringBuilder3 = stringBuilder2.Append(ref handler);
		try
		{
			foreach (ScriptBacktrace scriptBacktrace in scriptBacktraces)
			{
				if (!scriptBacktrace.IsEmpty())
				{
					stringBuilder = stringBuilder3;
					StringBuilder stringBuilder4 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder);
					handler.AppendFormatted(scriptBacktrace.Format(0, 4));
					stringBuilder4.Append(ref handler);
				}
			}
		}
		catch
		{
		}
		NLogWindow.AddLog(stringBuilder3.ToString());
		if (BaseLibConfig.OpenLogWindowOnError)
		{
			Callable val = Callable.From((Action)NLogWindow.OpenOnErr);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName._LogMessage, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("message"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("error"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._LogError, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("function"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("file"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("line"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("code"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("rationale"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("editorNotify"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("errorType"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)28, StringName.op_Implicit("scriptBacktraces"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._LogMessage && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			((Logger)this)._LogMessage(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._LogError && ((NativeVariantPtrArgs)(ref args)).Count == 8)
		{
			((Logger)this)._LogError(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[3]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[4]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[5]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[6]), VariantUtils.ConvertToArray<ScriptBacktrace>(ref ((NativeVariantPtrArgs)(ref args))[7]));
			ret = default(godot_variant);
			return true;
		}
		return ((Logger)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._LogMessage)
		{
			return true;
		}
		if ((ref method) == MethodName._LogError)
		{
			return true;
		}
		return ((Logger)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
	}
}
