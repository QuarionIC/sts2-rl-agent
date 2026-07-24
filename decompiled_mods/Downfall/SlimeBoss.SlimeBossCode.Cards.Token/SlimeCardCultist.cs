using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

public class SlimeCardCultist : SlimeCard<CultistSlime>
{
	public SlimeCardCultist()
		: base(showInCardLibrary: true, autoAdd: true)
	{
	}
}
