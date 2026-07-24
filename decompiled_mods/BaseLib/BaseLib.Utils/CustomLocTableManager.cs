using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Utils;

public static class CustomLocTableManager
{
	private static readonly HashSet<string> LocTables = new HashSet<string>();

	internal static IEnumerable<string> GetCustomLocTables(IEnumerable<string> original)
	{
		List<string> list = new List<string>();
		list.AddRange(original);
		list.AddRange(LocTables);
		return new _003C_003Ez__ReadOnlyList<string>(list);
	}

	public static void Register(string name)
	{
		if (!name.EndsWith(".json"))
		{
			name += ".json";
		}
		LocTables.Add(name);
	}

	public static void RegisterCustomLocTable(this LocManager locManager, string name)
	{
		Register(name);
	}
}
