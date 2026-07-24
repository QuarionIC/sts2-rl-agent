using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;

namespace Downfall.DownfallCode.CustomEnums;

public class CardBorderPath(string path)
{
	public string Path { get; } = path;

	public static implicit operator CardBorderPath(string text)
	{
		return new CardBorderPath(text);
	}

	public unsafe static implicit operator CardBorderPath(CardType keyword)
	{
		return new CardBorderPath(ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/card_portrait_border_" + ((object)(*(CardType*)(&keyword))/*cast due to .constrained prefix*/).ToString().ToLowerInvariant() + "_s.tres"));
	}
}
