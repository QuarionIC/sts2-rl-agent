using System.Collections.Generic;
using BaseLib.Patches.Content;

namespace BaseLib.Abstracts;

internal static class CustomCharacterSelectEntryRegistry
{
	public static readonly List<CustomCharacterSelectEntry> Entries = new List<CustomCharacterSelectEntry>();

	public static void Register(CustomCharacterSelectEntry entry)
	{
		if (CustomContentDictionary.RegisterType(entry.GetType()))
		{
			Entries.Add(entry);
			Entries.Sort(delegate(CustomCharacterSelectEntry a, CustomCharacterSelectEntry b)
			{
				int num = a.SortOrder.CompareTo(b.SortOrder);
				return (num == 0) ? string.CompareOrdinal(a.EntryId, b.EntryId) : num;
			});
		}
	}
}
