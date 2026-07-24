using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;

namespace BaseLib.Diagnostics;

internal static class HarmonyPatchDumpWriter
{
	internal static string? TryResolveFilesystemPath(string rawPath)
	{
		if (string.IsNullOrWhiteSpace(rawPath))
		{
			return null;
		}
		string text = rawPath.Trim();
		try
		{
			if (text.StartsWith("user://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
			{
				return ProjectSettings.GlobalizePath(text);
			}
			return Path.GetFullPath(text);
		}
		catch
		{
			return null;
		}
	}

	internal static bool TryWrite(string filesystemPath, out string? errorMessage)
	{
		errorMessage = null;
		try
		{
			string directoryName = Path.GetDirectoryName(filesystemPath);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			using FileStream stream = new FileStream(filesystemPath, FileMode.Create, FileAccess.Write, FileShare.Read);
			using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
			WriteReport(streamWriter);
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static void WriteReport(StreamWriter streamWriter)
	{
		streamWriter.WriteLine("=======================================================");
		streamWriter.WriteLine("===          Harmony Patch Dump Report             ===");
		streamWriter.WriteLine("=======================================================");
		streamWriter.WriteLine($"Generated at: {DateTime.Now:O}");
		streamWriter.WriteLine("User data dir: " + OS.GetUserDataDir());
		streamWriter.WriteLine("=======================================================");
		streamWriter.WriteLine();
		List<MethodBase> list = (from m in Harmony.GetAllPatchedMethods()
			orderby m.DeclaringType?.FullName ?? "Unknown", m.Name
			select m).ToList();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		foreach (MethodBase item in list)
		{
			num++;
			(int, int, int, int) tuple = LogPatchedMethodInfo(item, streamWriter);
			num2 += tuple.Item1;
			num3 += tuple.Item2;
			num4 += tuple.Item3;
			num5 += tuple.Item4;
			streamWriter.WriteLine();
		}
		streamWriter.WriteLine("=======================================================");
		streamWriter.WriteLine("===                   Summary                      ===");
		streamWriter.WriteLine("=======================================================");
		streamWriter.WriteLine($"Total Patched Methods:  {num}");
		streamWriter.WriteLine($"  - Prefix patches:     {num2}");
		streamWriter.WriteLine($"  - Postfix patches:    {num3}");
		streamWriter.WriteLine($"  - Transpiler patches: {num4}");
		streamWriter.WriteLine($"  - Finalizer patches:  {num5}");
		streamWriter.WriteLine($"  - Total patches:      {num2 + num3 + num4 + num5}");
		streamWriter.WriteLine("=======================================================");
	}

	private static (int prefixes, int postfixes, int transpilers, int finalizers) LogPatchedMethodInfo(MethodBase methodBase, StreamWriter streamWriter)
	{
		Patches patchInfo = Harmony.GetPatchInfo(methodBase);
		if (patchInfo == null)
		{
			return (prefixes: 0, postfixes: 0, transpilers: 0, finalizers: 0);
		}
		string text = methodBase.DeclaringType?.FullName ?? "Unknown";
		string methodSignature = GetMethodSignature(methodBase);
		string text2 = ((methodBase is MethodInfo methodInfo) ? methodInfo.ReturnType.Name : "void");
		streamWriter.WriteLine("┌─ [" + text + "]");
		streamWriter.WriteLine("│  Method: " + text2 + " " + methodSignature);
		streamWriter.WriteLine("│");
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (patchInfo.Prefixes.Count > 0)
		{
			streamWriter.WriteLine($"│  ├─ Prefixes ({patchInfo.Prefixes.Count}):");
			foreach (Patch item in from p in patchInfo.Prefixes
				orderby p.priority, p.owner
				select p)
			{
				streamWriter.WriteLine("│  │  " + FormatPatchInfo(item));
				num++;
			}
		}
		if (patchInfo.Postfixes.Count > 0)
		{
			streamWriter.WriteLine($"│  ├─ Postfixes ({patchInfo.Postfixes.Count}):");
			foreach (Patch item2 in from p in patchInfo.Postfixes
				orderby p.priority, p.owner
				select p)
			{
				streamWriter.WriteLine("│  │  " + FormatPatchInfo(item2));
				num2++;
			}
		}
		if (patchInfo.Transpilers.Count > 0)
		{
			streamWriter.WriteLine($"│  ├─ Transpilers ({patchInfo.Transpilers.Count}):");
			foreach (Patch item3 in from p in patchInfo.Transpilers
				orderby p.priority, p.owner
				select p)
			{
				streamWriter.WriteLine("│  │  " + FormatPatchInfo(item3));
				num3++;
			}
		}
		if (patchInfo.Finalizers.Count > 0)
		{
			streamWriter.WriteLine($"│  └─ Finalizers ({patchInfo.Finalizers.Count}):");
			foreach (Patch item4 in from p in patchInfo.Finalizers
				orderby p.priority, p.owner
				select p)
			{
				streamWriter.WriteLine("│     " + FormatPatchInfo(item4));
				num4++;
			}
		}
		streamWriter.WriteLine("└─────────────────────────────────────────────────────────────────");
		return (prefixes: num, postfixes: num2, transpilers: num3, finalizers: num4);
	}

	private static string GetMethodSignature(MethodBase methodBase)
	{
		ParameterInfo[] parameters = methodBase.GetParameters();
		string text = string.Join(", ", parameters.Select((ParameterInfo p) => p.ParameterType.Name + " " + p.Name));
		return methodBase.Name + "(" + text + ")";
	}

	private static string FormatPatchInfo(Patch patch)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("├─ [Priority: ");
		handler.AppendFormatted(patch.priority);
		handler.AppendLiteral("] ");
		stringBuilder3.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
		handler.AppendLiteral("[");
		handler.AppendFormatted(patch.owner);
		handler.AppendLiteral("] ");
		stringBuilder4.Append(ref handler);
		string value = patch.PatchMethod.DeclaringType?.FullName ?? "Unknown";
		string name = patch.PatchMethod.Name;
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2);
		handler.AppendFormatted(value);
		handler.AppendLiteral(".");
		handler.AppendFormatted(name);
		stringBuilder5.Append(ref handler);
		try
		{
			string fileName = Path.GetFileName(patch.PatchMethod.Module.FullyQualifiedName);
			if (!string.IsNullOrEmpty(fileName) && fileName != "<Unknown>")
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
				handler.AppendLiteral(" (from ");
				handler.AppendFormatted(fileName);
				handler.AppendLiteral(")");
				stringBuilder6.Append(ref handler);
			}
		}
		catch
		{
		}
		return stringBuilder.ToString();
	}
}
