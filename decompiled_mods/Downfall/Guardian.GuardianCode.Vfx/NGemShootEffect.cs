using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Commands;

namespace Guardian.GuardianCode.Vfx;

[ScriptPath("res://GuardianCode/Vfx/NGemShootEffect.cs")]
public class NGemShootEffect : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnLanded = StringName.op_Implicit("OnLanded");

		public static readonly StringName PlayHitSound = StringName.op_Implicit("PlayHitSound");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _from = StringName.op_Implicit("_from");

		public static readonly StringName _hitNo = StringName.op_Implicit("_hitNo");

		public static readonly StringName _sprite = StringName.op_Implicit("_sprite");

		public static readonly StringName _target = StringName.op_Implicit("_target");

		public static readonly StringName _total = StringName.op_Implicit("_total");
	}

	public class SignalName : SignalName
	{
	}

	private Vector2 _from;

	private GemModel? _gem;

	private int _hitNo;

	private Sprite2D? _sprite;

	private Vector2 _target;

	private int _total;

	public static NGemShootEffect Create(GemModel gem, int hitNo, Vector2 from, Vector2 target, int total)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return new NGemShootEffect
		{
			_gem = gem,
			_from = from,
			_target = target,
			_hitNo = hitNo,
			_total = total
		};
	}

	public override void _Ready()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		if (_gem != null)
		{
			_sprite = new Sprite2D();
			_sprite.Texture = _gem.Icon;
			((Node2D)_sprite).Scale = new Vector2(0.5f, 0.5f);
			((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
			((Node2D)this).GlobalPosition = _from;
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(_from.X + (float)GD.RandRange(-200.0, 200.0), _from.Y + (float)GD.RandRange(-200.0, 200.0));
			float num = (float)_hitNo * 0.2f;
			Tween obj = ((Node)this).CreateTween().SetParallel(true);
			obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("global_position"), Variant.op_Implicit(val), 0.30000001192092896).SetDelay((double)num).SetTrans((TransitionType)1)
				.SetEase((EaseType)1);
			obj.Chain();
			obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("global_position"), Variant.op_Implicit(_target), 0.30000001192092896).SetTrans((TransitionType)4).SetEase((EaseType)0);
			obj.TweenProperty((GodotObject)(object)_sprite, NodePath.op_Implicit("scale"), Variant.op_Implicit(new Vector2(0.7f, 0.35f)), 0.30000001192092896);
			obj.Chain();
			obj.TweenCallback(Callable.From((Action)OnLanded));
		}
	}

	private void OnLanded()
	{
		((CanvasItem)this).Visible = false;
		PlayHitSound();
		((Node)this).QueueFree();
	}

	private void PlayHitSound()
	{
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack", 1f);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnLanded, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.PlayHitSound, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnLanded && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnLanded();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.PlayHitSound && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			PlayHitSound();
			ret = default(godot_variant);
			return true;
		}
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnLanded)
		{
			return true;
		}
		if ((ref method) == MethodName.PlayHitSound)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._from)
		{
			_from = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hitNo)
		{
			_hitNo = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			_sprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._target)
		{
			_target = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._total)
		{
			_total = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._from)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _from);
			return true;
		}
		if ((ref name) == PropertyName._hitNo)
		{
			value = VariantUtils.CreateFrom<int>(ref _hitNo);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _sprite);
			return true;
		}
		if ((ref name) == PropertyName._target)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _target);
			return true;
		}
		if ((ref name) == PropertyName._total)
		{
			value = VariantUtils.CreateFrom<int>(ref _total);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)5, PropertyName._from, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._hitNo, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._sprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._target, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._total, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._from, Variant.From<Vector2>(ref _from));
		info.AddProperty(PropertyName._hitNo, Variant.From<int>(ref _hitNo));
		info.AddProperty(PropertyName._sprite, Variant.From<Sprite2D>(ref _sprite));
		info.AddProperty(PropertyName._target, Variant.From<Vector2>(ref _target));
		info.AddProperty(PropertyName._total, Variant.From<int>(ref _total));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._from, ref val))
		{
			_from = ((Variant)(ref val)).As<Vector2>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._hitNo, ref val2))
		{
			_hitNo = ((Variant)(ref val2)).As<int>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._sprite, ref val3))
		{
			_sprite = ((Variant)(ref val3)).As<Sprite2D>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._target, ref val4))
		{
			_target = ((Variant)(ref val4)).As<Vector2>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._total, ref val5))
		{
			_total = ((Variant)(ref val5)).As<int>();
		}
	}
}
