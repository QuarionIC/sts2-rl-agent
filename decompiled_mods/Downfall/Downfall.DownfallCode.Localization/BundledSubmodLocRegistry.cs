using System.Collections.Generic;

namespace Downfall.DownfallCode.Localization;

public static class BundledSubmodLocRegistry
{
	private static readonly HashSet<string> _ids = new HashSet<string>();

	public static IReadOnlyCollection<string> Ids => _ids;

	public static void Register(string modId)
	{
		_ids.Add(modId);
	}
}
