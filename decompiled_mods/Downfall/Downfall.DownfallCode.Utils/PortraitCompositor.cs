using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Downfall.DownfallCode.Utils;

public static class PortraitCompositor
{
	public static ImageTexture? SliceHorizontally(IReadOnlyList<Texture2D?> textures)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Invalid comparison between Unknown and I8
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		List<Image> list = (from img in textures.Select(ExtractImage).OfType<Image>()
			where !img.IsEmpty()
			select img).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		int width = list[0].GetWidth();
		int height = list[0].GetHeight();
		Image val = Image.CreateEmpty(width, height, false, (Format)5);
		int num = width / list.Count;
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			Image val2 = list[num2];
			if ((long)val2.GetFormat() != 5)
			{
				val2.Convert((Format)5);
			}
			if (val2.GetWidth() != width || val2.GetHeight() != height)
			{
				val2.Resize(width, height, (Interpolation)1);
			}
			int num3 = ((num2 == list.Count - 1) ? (width - num2 * num) : num);
			val.BlitRect(val2, new Rect2I(num2 * num, 0, num3, height), new Vector2I(num2 * num, 0));
		}
		return ImageTexture.CreateFromImage(val);
	}

	private static Image? ExtractImage(Texture2D? texture)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (texture != null)
		{
			AtlasTexture val = (AtlasTexture)(object)((texture is AtlasTexture) ? texture : null);
			if (val != null && val.Atlas != null)
			{
				Image val2 = ExtractImage(val.Atlas);
				if (val2 == null || val2.IsEmpty())
				{
					return null;
				}
				Rect2 region = val.Region;
				Rect2I val3 = default(Rect2I);
				((Rect2I)(ref val3))._002Ector((int)((Rect2)(ref region)).Position.X, (int)((Rect2)(ref region)).Position.Y, (int)((Rect2)(ref region)).Size.X, (int)((Rect2)(ref region)).Size.Y);
				return val2.GetRegion(val3);
			}
			Image image = texture.GetImage();
			if (image == null || image.IsEmpty())
			{
				return null;
			}
			if (image.IsCompressed())
			{
				image.Decompress();
			}
			return image;
		}
		return null;
	}
}
