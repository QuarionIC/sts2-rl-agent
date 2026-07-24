using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Champ.ChampCode.Vfx;

[GlobalClass]
[ScriptPath("res://ChampCode/Vfx/NChampCreatureVisuals.cs")]
public class NChampCreatureVisuals : NCreatureVisuals, IAnimatedVisuals
{
	public enum Stance
	{
		Normal,
		Berserker,
		Defensive,
		Ultimate
	}

	public class MethodName : MethodName
	{
		public static readonly StringName OnAnimationTrigger = StringName.op_Implicit("OnAnimationTrigger");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName CurrentStance = StringName.op_Implicit("CurrentStance");

		public static readonly StringName IdleAnim = StringName.op_Implicit("IdleAnim");

		public static readonly StringName AttackAnim = StringName.op_Implicit("AttackAnim");

		public static readonly StringName HitAnim = StringName.op_Implicit("HitAnim");
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

	public Stance CurrentStance { get; set; }

	private string IdleAnim => CurrentStance switch
	{
		Stance.Berserker => "IdleBerserker", 
		Stance.Defensive => "IdleDefensive", 
		Stance.Ultimate => "IdleUltimate", 
		_ => "Idle", 
	};

	private string AttackAnim
	{
		get
		{
			_ = CurrentStance;
			return "Attack";
		}
	}

	private string HitAnim => CurrentStance switch
	{
		Stance.Berserker => "HitBerserker", 
		Stance.Defensive => "HitDefensive", 
		_ => "Hit", 
	};

	public void OnAnimationTrigger(string trigger)
	{
		switch (trigger)
		{
		default:
			_ = trigger == "Dead";
			break;
		case "Idle":
			_animState?.SetAnimationWithMix(IdleAnim, 0.2f);
			break;
		case "Attack":
			_animState?.SetAnimationWithMix(AttackAnim, 0.1f, loop: false);
			_animState?.QueueAnimation(IdleAnim, 0.35f);
			break;
		case "Hit":
			_animState?.SetAnimationWithMix(HitAnim, 0.05f, loop: false);
			_animState?.QueueAnimation(IdleAnim, 0.35f);
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
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.CurrentStance)
		{
			CurrentStance = VariantUtils.ConvertTo<Stance>(ref value);
			return true;
		}
		return ((NCreatureVisuals)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.CurrentStance)
		{
			Stance currentStance = CurrentStance;
			value = VariantUtils.CreateFrom<Stance>(ref currentStance);
			return true;
		}
		if ((ref name) == PropertyName.IdleAnim)
		{
			string idleAnim = IdleAnim;
			value = VariantUtils.CreateFrom<string>(ref idleAnim);
			return true;
		}
		if ((ref name) == PropertyName.AttackAnim)
		{
			string idleAnim = AttackAnim;
			value = VariantUtils.CreateFrom<string>(ref idleAnim);
			return true;
		}
		if ((ref name) == PropertyName.HitAnim)
		{
			string idleAnim = HitAnim;
			value = VariantUtils.CreateFrom<string>(ref idleAnim);
			return true;
		}
		return ((NCreatureVisuals)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName.CurrentStance, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.IdleAnim, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.AttackAnim, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.HitAnim, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((NCreatureVisuals)this).SaveGodotObjectData(info);
		StringName currentStance = PropertyName.CurrentStance;
		Stance currentStance2 = CurrentStance;
		info.AddProperty(currentStance, Variant.From<Stance>(ref currentStance2));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCreatureVisuals)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.CurrentStance, ref val))
		{
			CurrentStance = ((Variant)(ref val)).As<Stance>();
		}
	}
}
