using System;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

[Obsolete]
public class SlimeCardScrap : SlimeCard<ScrapSlime>
{
	public SlimeCardScrap()
		: base(showInCardLibrary: false, autoAdd: false)
	{
	}
}
