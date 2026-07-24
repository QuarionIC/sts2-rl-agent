using System;

namespace BaseLib.Extensions;

public static class TypePrefix
{
	public const char PrefixSplitChar = '-';

	public static string GetPrefix(this Type t)
	{
		if (t.Namespace == null)
		{
			return "";
		}
		int num = t.Namespace.IndexOf('.');
		if (num == -1)
		{
			num = t.Namespace.Length;
		}
		return $"{t.Namespace.Substring(0, num).ToUpperInvariant()}{45}";
	}

	public static string GetRootNamespace(this Type t)
	{
		if (t.Namespace == null)
		{
			return "";
		}
		int num = t.Namespace.IndexOf('.');
		if (num == -1)
		{
			num = t.Namespace.Length;
		}
		return t.Namespace.Substring(0, num);
	}
}
