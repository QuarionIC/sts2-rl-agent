using System;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

[Obsolete]
public class SlimeCardGreed : SlimeCard<GreedSlime>
{
	public SlimeCardGreed()
		: base(showInCardLibrary: false, autoAdd: false)
	{
	}
}
