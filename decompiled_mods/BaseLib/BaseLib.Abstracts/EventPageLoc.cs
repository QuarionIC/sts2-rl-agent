using System.Collections.Generic;

namespace BaseLib.Abstracts;

public record EventPageLoc(string PageKey, string Description, params EventOptionLoc[] Options)
{
	public static implicit operator List<(string, string)>(EventPageLoc loc)
	{
		List<(string, string)> list = new List<(string, string)>();
		list.Add(("pages." + loc.PageKey + ".description", loc.Description));
		EventOptionLoc[] options = loc.Options;
		foreach (EventOptionLoc eventOptionLoc in options)
		{
			list.AddRange(eventOptionLoc.Create(loc));
		}
		return list;
	}
}
