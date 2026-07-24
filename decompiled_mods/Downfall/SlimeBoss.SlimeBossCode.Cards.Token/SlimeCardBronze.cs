using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

public class SlimeCardBronze : SlimeCard<BronzeSlime>
{
	public SlimeCardBronze()
		: base(showInCardLibrary: true, autoAdd: true)
	{
	}
}
