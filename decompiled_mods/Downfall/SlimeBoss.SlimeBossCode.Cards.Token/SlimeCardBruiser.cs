using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

public class SlimeCardBruiser : SlimeCard<BruiserSlime>
{
	public SlimeCardBruiser()
		: base(showInCardLibrary: true, autoAdd: true)
	{
	}
}
