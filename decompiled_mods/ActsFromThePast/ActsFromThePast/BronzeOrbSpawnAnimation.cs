using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class BronzeOrbSpawnAnimation
{
	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/obscura/obscura_buff", 1f);
		NCreatureVisuals visuals = creatureNode.Visuals;
		if (visuals == null)
		{
			return;
		}
		Sprite2D sprite = ((Node)visuals).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Visuals"));
		if (sprite != null)
		{
			Shader shader = new Shader();
			shader.Code = "\r\nshader_type canvas_item;\r\n\r\nuniform float progress : hint_range(0.0, 1.0) = 0.0;\r\nuniform float hologram_strength : hint_range(0.0, 1.0) = 1.0;\r\nuniform vec3 hologram_color : source_color = vec3(0.2, 0.45, 1.0);\r\nuniform vec3 scanline_color : source_color = vec3(0.0, 1.0, 0.15);\r\nuniform float scanline_width : hint_range(0.0, 0.2) = 0.06;\r\nuniform float scanline_extend : hint_range(0.0, 0.5) = 0.15;\r\n\r\nvoid fragment() {\r\n    vec4 tex = texture(TEXTURE, UV);\r\n    float y = 1.0 - UV.y;\r\n\r\n    // Holographic horizontal scanlines\r\n    float scan = sin(UV.y * 120.0 + TIME * 3.0) * 0.5 + 0.5;\r\n    float scan_fine = sin(UV.y * 300.0 - TIME * 8.0) * 0.5 + 0.5;\r\n\r\n    // Subtle holographic flicker\r\n    float flicker = sin(TIME * 12.0) * 0.03 + 0.97;\r\n\r\n    if (y > progress) {\r\n        COLOR.a = 0.0;\r\n    } else {\r\n        // Base hologram tint\r\n        vec3 tinted = mix(tex.rgb, hologram_color, hologram_strength * 0.65);\r\n\r\n        // Add scanline interference pattern when holographic\r\n        tinted += hologram_strength * vec3(0.05, 0.1, 0.2) * scan * 0.4;\r\n        tinted += hologram_strength * vec3(0.02, 0.05, 0.1) * scan_fine * 0.3;\r\n\r\n        // Flicker\r\n        tinted *= mix(1.0, flicker, hologram_strength);\r\n\r\n        // Slight edge glow — boost alpha near texture edges\r\n        float edge = smoothstep(0.0, 0.15, tex.a) * smoothstep(1.0, 0.85, tex.a);\r\n        tinted += hologram_strength * hologram_color * (1.0 - edge) * 0.3;\r\n\r\n        COLOR.rgb = tinted;\r\n        COLOR.a = tex.a;\r\n\r\n// Green scanline at the progress boundary\r\n    float dist = abs(y - progress);\r\n    float top_fade = smoothstep(1.0, 0.85, progress);\r\n    if (dist < scanline_width) {\r\n        float glow = 1.0 - (dist / scanline_width);\r\n        glow = glow * glow;\r\n        glow *= top_fade;\r\n        COLOR.rgb = mix(COLOR.rgb, scanline_color, glow * 0.9);\r\n        COLOR.a = max(COLOR.a, glow * 0.7);\r\n    }\r\n    }\r\n\r\n// Extend green scanline beyond texture horizontally\r\n    float dist_to_line = abs((1.0 - UV.y) - progress);\r\n    float top_fade_ext = smoothstep(1.0, 0.85, progress);\r\n    if (dist_to_line < scanline_width && tex.a < 0.01) {\r\n        float center_dist = abs(UV.x - 0.5);\r\n        if (center_dist < 0.5 + scanline_extend) {\r\n            float h_falloff = smoothstep(0.5 + scanline_extend, 0.35, center_dist);\r\n            float v_glow = 1.0 - (dist_to_line / scanline_width);\r\n            v_glow = v_glow * v_glow;\r\n            v_glow *= top_fade_ext;\r\n            COLOR.rgb = scanline_color;\r\n            COLOR.a = v_glow * h_falloff * 0.6;\r\n        }\r\n    }\r\n}\r\n";
			ShaderMaterial material = new ShaderMaterial
			{
				Shader = shader
			};
			material.SetShaderParameter(StringName.op_Implicit("progress"), Variant.op_Implicit(0f));
			material.SetShaderParameter(StringName.op_Implicit("hologram_strength"), Variant.op_Implicit(1f));
			((CanvasItem)sprite).Material = (Material)(object)material;
			((CanvasItem)sprite).Visible = true;
			Tween tween = ((Node)creatureNode).CreateTween();
			tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float p)
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				material.SetShaderParameter(StringName.op_Implicit("progress"), Variant.op_Implicit(p));
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 1.5).SetTrans((TransitionType)0);
			await Cmd.Wait(1.5f, false);
			Tween fadeTween = ((Node)creatureNode).CreateTween();
			fadeTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float h)
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				material.SetShaderParameter(StringName.op_Implicit("hologram_strength"), Variant.op_Implicit(h));
			}), Variant.op_Implicit(1f), Variant.op_Implicit(0f), 0.5).SetTrans((TransitionType)1).SetEase((EaseType)1);
			await Cmd.Wait(0.5f, false);
			((CanvasItem)sprite).Material = null;
		}
	}
}
