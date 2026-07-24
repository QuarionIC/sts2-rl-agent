using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Cards;

public class BaseLibKeywords
{
	[CustomEnum(null)]
	[KeywordProperties(AutoKeywordPosition.After)]
	public static CardKeyword Purge;
}
