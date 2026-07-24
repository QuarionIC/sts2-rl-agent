using System.Collections.Generic;

namespace BaseLib.Abstracts;

public record EventOptionLoc(string OptionKey, string Title, string Description)
{
	public IEnumerable<(string, string)> Create(EventPageLoc page)
	{
		yield return ($"pages.{page.PageKey}.options.{OptionKey}.title", Title);
		yield return ($"pages.{page.PageKey}.options.{OptionKey}.description", Description);
	}
}
