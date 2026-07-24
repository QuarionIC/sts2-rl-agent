using System;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

[Obsolete]
public class SlimeCardDarkling : SlimeCard<DarklingSlime>
{
	public SlimeCardDarkling()
		: base(showInCardLibrary: false, autoAdd: false)
	{
	}
}
