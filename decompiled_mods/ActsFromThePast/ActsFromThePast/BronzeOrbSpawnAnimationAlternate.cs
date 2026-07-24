using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class BronzeOrbSpawnAnimationAlternate
{
	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		NCreatureVisuals visuals = creatureNode.Visuals;
		if (visuals == null)
		{
			return;
		}
		Sprite2D sprite = ((Node)visuals).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Visuals"));
		if (sprite != null)
		{
			((CanvasItem)sprite).Visible = false;
			Shader shader = new Shader();
			shader.Code = "\r\nshader_type canvas_item;\r\n\r\nuniform float progress : hint_range(0.0, 1.0) = 0.0;\r\nuniform float hologram_strength : hint_range(0.0, 1.0) = 1.0;\r\nuniform vec3 hologram_color : source_color = vec3(0.3, 0.5, 1.0);\r\nuniform vec3 scanline_color : source_color = vec3(0.2, 1.0, 0.4);\r\nuniform float scanline_width : hint_range(0.0, 0.2) = 0.06;\r\n\r\nvoid fragment() {\r\n    vec4 tex = texture(TEXTURE, UV);\r\n    float y = 1.0 - UV.y;\r\n\r\n    if (y > progress) {\r\n        COLOR.a = 0.0;\r\n    } else {\r\n        vec3 tinted = mix(tex.rgb, hologram_color, hologram_strength * 0.6);\r\n        COLOR.rgb = tinted;\r\n        COLOR.a = tex.a;\r\n\r\n        float dist = abs(y - progress);\r\n        if (dist < scanline_width) {\r\n            float glow = 1.0 - (dist / scanline_width);\r\n            COLOR.rgb = mix(COLOR.rgb, scanline_color, glow * 0.8);\r\n        }\r\n    }\r\n}\r\n";
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
