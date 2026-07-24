using Downfall.DownfallCode.Interfaces;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

internal static class CustomPortraitApplier
{
	internal static void Apply(NCard nCard)
	{
		if (nCard.Model is ICustomPortrait customPortrait && nCard._portrait != null)
		{
			Texture2D portraitTexture = customPortrait.GetPortraitTexture();
			if (portraitTexture != null)
			{
				nCard._portrait.Texture = portraitTexture;
			}
		}
	}
}
