using System.Collections.Generic;
using System.Linq;
using Downfall.DownfallCode.Artists;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NHoverTipSet), "Init")]
internal static class AddArtistHoverTipPatch
{
	private static void Prefix(NHoverTipSet __instance, ref IEnumerable<IHoverTip> hoverTips)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		List<IHoverTip> source = hoverTips.ToList();
		hoverTips = (from tip in source
			where !(tip is ArtistHoverTip)
			orderby (tip is ArtistHoverTip) ? 1 : 0
			select tip).ToList();
		if (1 == 0)
		{
			return;
		}
		foreach (ArtistHoverTip item in source.OfType<ArtistHoverTip>())
		{
			Control val = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip.tscn").Instantiate<Control>((GenEditState)0);
			GodotTreeExtensions.AddChildSafely((Node)(object)__instance._textHoverTipContainer, (Node)(object)val);
			((Node)val).GetNode<MegaLabel>(NodePath.op_Implicit("%Title")).SetTextAutoSize(item.Title.GetFormattedText());
			((Node)val).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("%Description")).Text = "";
			((RichTextLabel)((Node)val).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("%Description"))).AutowrapMode = (AutowrapMode)3;
			((Node)val).GetNode<TextureRect>(NodePath.op_Implicit("%Icon")).Texture = item.Icon;
			CanvasItem nodeOrNull = ((Node)val).GetNodeOrNull<CanvasItem>(NodePath.op_Implicit("Bg"));
			if (nodeOrNull != null)
			{
				ShaderMaterial val2 = new ShaderMaterial();
				val2.Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader", (string)null, (CacheMode)1);
				val2.SetShaderParameter(StringName.op_Implicit("h"), Variant.op_Implicit(0.6f));
				val2.SetShaderParameter(StringName.op_Implicit("s"), Variant.op_Implicit(4f));
				val2.SetShaderParameter(StringName.op_Implicit("v"), Variant.op_Implicit(1f));
				nodeOrNull.Material = (Material)(object)val2;
			}
			val.ResetSize();
			if (NGame.Instance != null)
			{
				double num = (double)(((Control)__instance._textHoverTipContainer).Size.Y + val.Size.Y) + 5.0;
				Rect2 viewportRect = ((CanvasItem)NGame.Instance).GetViewportRect();
				if (num < (double)((Rect2)(ref viewportRect)).Size.Y - 50.0)
				{
					((Control)__instance._textHoverTipContainer).Size = new Vector2(360f, ((Control)__instance._textHoverTipContainer).Size.Y + val.Size.Y + 5f);
					continue;
				}
			}
			((FlowContainer)__instance._textHoverTipContainer).Alignment = (AlignmentMode)1;
		}
	}
}
