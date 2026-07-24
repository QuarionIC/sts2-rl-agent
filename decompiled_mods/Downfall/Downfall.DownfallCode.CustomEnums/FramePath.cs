using MegaCrit.Sts2.Core.Entities.Cards;

namespace Downfall.DownfallCode.CustomEnums;

public class FramePath(string path)
{
	public string Path { get; } = path;

	public static implicit operator FramePath(string text)
	{
		return new FramePath(text);
	}

	public unsafe static implicit operator FramePath(CardType keyword)
	{
		return new FramePath("atlases/ui_atlas.sprites/card/card_frame_" + ((object)(*(CardType*)(&keyword))/*cast due to .constrained prefix*/).ToString().ToLowerInvariant() + "_s.tres");
	}
}
