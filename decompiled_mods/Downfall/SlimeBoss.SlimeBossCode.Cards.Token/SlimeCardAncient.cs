using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

public class SlimeCardAncient : SlimeCard<AncientSlime>
{
	public SlimeCardAncient()
		: base(showInCardLibrary: true, autoAdd: true)
	{
	}
}
