using Downfall.DownfallCode.Interfaces;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardColorPatch
{
	public static void Postfix(NCard __instance)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (__instance.Model is IColoredPortrait coloredPortrait)
		{
			TextureRect nodeOrNull = ((Node)__instance).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("%Portrait"));
			if (nodeOrNull != null)
			{
				ShaderMaterial val = new ShaderMaterial();
				val.Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader", (string)null, (CacheMode)1);
				val.SetShaderParameter(StringName.op_Implicit("h"), Variant.op_Implicit(coloredPortrait.HueShift));
				val.SetShaderParameter(StringName.op_Implicit("s"), Variant.op_Implicit(coloredPortrait.Saturation));
				val.SetShaderParameter(StringName.op_Implicit("v"), Variant.op_Implicit(coloredPortrait.Value));
				((CanvasItem)nodeOrNull).Material = (Material)(object)val;
			}
		}
	}
}
