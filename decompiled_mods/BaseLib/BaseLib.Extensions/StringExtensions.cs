using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using BaseLib.Utils.NodeFactories;
using Godot;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class StringExtensions
{
	private static readonly HashAlgorithm MD5 = System.Security.Cryptography.MD5.Create();

	private static readonly Dictionary<string, int> HashDict = new Dictionary<string, int>();

	private static readonly HashSet<int> ExistingHashes = new HashSet<int>();

	public static string RemovePrefix(this string id)
	{
		int num = id.IndexOf('-') + 1;
		return id.Substring(num, id.Length - num);
	}

	public static void RegisterSceneForConversion<TNode>(this string scenePath, Action<TNode>? postConversion = null) where TNode : Node
	{
		NodeFactory.RegisterSceneType(scenePath, postConversion);
	}

	internal static IEnumerable<CodeInstruction> MakeWriteLog(this string s)
	{
		yield return new CodeInstruction(OpCodes.Ldstr, (object)s);
		yield return CodeInstruction.Call(typeof(StringExtensions), "WriteLog", (Type[])null, (Type[])null);
	}

	internal static void WriteLog(string s)
	{
		BaseLibMain.Logger.Info(s, 1);
	}

	internal static void WriteLogInt(int i)
	{
		BaseLibMain.Logger.Info(i.ToString(), 1);
	}

	internal static void WriteLogObj(object? o)
	{
		BaseLibMain.Logger.Info(o?.ToString() ?? "NULL", 1);
	}

	public static int ComputeBasicHash(this string s)
	{
		if (!HashDict.TryGetValue(s, out var value))
		{
			byte[] array = MD5.ComputeHash(Encoding.UTF8.GetBytes(s));
			value = -2128831035;
			for (int i = 0; i < array.Length; i++)
			{
				value = (value ^ array[i]) * 16777619;
			}
			HashDict[s] = value;
			if (ExistingHashes.Add(value))
			{
				return value;
			}
			{
				foreach (KeyValuePair<string, int> item in HashDict)
				{
					if (item.Value.Equals(value))
					{
						BaseLibMain.Logger.Warn($"Duplicate hashes for {item.Key} and {s}: {value}", 1);
					}
				}
				return value;
			}
		}
		return value;
	}

	public static Type? TryGetType(this string typeName)
	{
		try
		{
			return Type.GetType(typeName + ", sts2");
		}
		catch (Exception)
		{
		}
		return null;
	}
}
