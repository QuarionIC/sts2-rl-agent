using System.Collections.Generic;
using System.Linq;

namespace BaseLib.Abstracts;

public record EventLoc(string Title, params EventPageLoc[] Pages)
{
	public static implicit operator List<(string, string)>(EventLoc loc)
	{
		List<(string, string)> list = new List<(string, string)>();
		list.Add(("title", loc.Title));
		list.AddRange(loc.Pages.SelectMany((EventPageLoc page) => (List<(string, string)>)page));
		return list;
	}
}
