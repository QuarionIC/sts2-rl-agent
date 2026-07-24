using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Champ.ChampCode.CustomEnums;

public static class ChampTag
{
	[CustomEnum(null)]
	public static CardTag Finisher;

	[CustomEnum(null)]
	public static CardTag EnterDefensive;

	[CustomEnum(null)]
	public static CardTag EnterBerserker;
}
