using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

public class SlimeCardLeeching : SlimeCard<LeechingSlime>
{
	public SlimeCardLeeching()
		: base(showInCardLibrary: true, autoAdd: true)
	{
	}
}
