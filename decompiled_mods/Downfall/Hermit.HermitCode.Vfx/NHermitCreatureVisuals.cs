using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hermit.HermitCode.Vfx;

[GlobalClass]
[ScriptPath("res://HermitCode/Vfx/NHermitCreatureVisuals.cs")]
public class NHermitCreatureVisuals : NCreatureVisuals, IAnimatedVisuals
{
	public class MethodName : MethodName
	{
		public static readonly StringName OnAnimationTrigger = StringName.op_Implicit("OnAnimationTrigger");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	private const float DefaultMix = 0.2f;

	private const float ToIdleMix = 0.35f;

	private const float AttackMix = 0.1f;

	private const float HitMix = 0.05f;

	private MegaAnimationState? _animState;

	private MegaSprite? _sprite;

	public void OnAnimationTrigger(string trigger)
	{
		switch (trigger)
		{
		default:
			_ = trigger == "Dead";
			break;
		case "Idle":
			_animState?.SetAnimationWithMix("Idle", 0.2f);
			break;
		case "Hit":
			_animState?.SetAnimationWithMix("Hit", 0.05f, loop: false);
			_animState?.QueueAnimation("Idle", 0.35f);
			break;
		case "Attack":
			break;
		case "Cast":
			break;
		}
	}

	public override void _Ready()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		((NCreatureVisuals)this)._Ready();
		CanvasItemMaterial normalMaterial = new CanvasItemMaterial
		{
			BlendMode = (BlendModeEnum)4
		};
		_sprite = ((NCreatureVisuals)this).SpineBody;
		MegaSprite? sprite = _sprite;
		if (sprite != null)
		{
			sprite.SetNormalMaterial((Material)(object)normalMaterial);
		}
		MegaSprite? sprite2 = _sprite;
		_animState = ((sprite2 != null) ? sprite2.GetAnimationState() : null);
		_animState?.SetAnimationCompat("Idle");
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName.OnAnimationTrigger, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("trigger"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.OnAnimationTrigger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnAnimationTrigger(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((NCreatureVisuals)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.OnAnimationTrigger)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((NCreatureVisuals)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((NCreatureVisuals)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCreatureVisuals)this).RestoreGodotObjectData(info);
	}
}
