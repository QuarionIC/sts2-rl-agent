using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Downfall.DownfallCode.Vfx;

[GlobalClass]
[ScriptPath("res://DownfallCode/Vfx/NStatusPart.cs")]
public class NStatusPart : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _foregroundTween = StringName.op_Implicit("_foregroundTween");

		public static readonly StringName _hpForeground = StringName.op_Implicit("_hpForeground");

		public static readonly StringName _hpMiddleground = StringName.op_Implicit("_hpMiddleground");

		public static readonly StringName _showHideTween = StringName.op_Implicit("_showHideTween");
	}

	public class SignalName : SignalName
	{
	}

	private Tween? _foregroundTween;

	private NinePatchRect _hpForeground;

	private NinePatchRect _hpMiddleground;

	private Tween? _showHideTween;

	public override void _Ready()
	{
		_hpForeground = ((Node)this).GetNode<NinePatchRect>(NodePath.op_Implicit("ForegroundContainer/Mask/HpForeground"));
		_hpMiddleground = ((Node)this).GetNode<NinePatchRect>(NodePath.op_Implicit("ForegroundContainer/Mask/HpMiddleground"));
	}

	public void Show(bool filled, Color? color = null)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Callable val = Callable.From((Action)delegate
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			Callable val2 = Callable.From((Action)delegate
			{
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				Callable val3 = Callable.From((Action)delegate
				{
					SetFill(filled, color);
				});
				((Callable)(ref val3)).CallDeferred(Array.Empty<Variant>());
			});
			((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	public void SetFill(bool fill, Color? color = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (color.HasValue)
		{
			((CanvasItem)_hpForeground).SelfModulate = color.Value;
		}
		float x = ((Control)this).Size.X;
		Tween? foregroundTween = _foregroundTween;
		if (foregroundTween != null)
		{
			foregroundTween.Kill();
		}
		_foregroundTween = ((Node)this).CreateTween();
		((Control)_hpMiddleground).OffsetRight = ((Control)_hpForeground).OffsetRight;
		if (fill)
		{
			_foregroundTween.TweenProperty((GodotObject)(object)_hpForeground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0), 0.3).SetEase((EaseType)1).SetTrans((TransitionType)5);
			_foregroundTween.TweenProperty((GodotObject)(object)_hpMiddleground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0), 0.1).SetEase((EaseType)1).SetTrans((TransitionType)5);
		}
		else
		{
			_foregroundTween.TweenProperty((GodotObject)(object)_hpForeground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0f - x), 0.3).SetEase((EaseType)1).SetTrans((TransitionType)5);
			_foregroundTween.TweenProperty((GodotObject)(object)_hpMiddleground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0f - x), 0.3).SetEase((EaseType)1).SetTrans((TransitionType)5);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._foregroundTween)
		{
			_foregroundTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hpForeground)
		{
			_hpForeground = VariantUtils.ConvertTo<NinePatchRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hpMiddleground)
		{
			_hpMiddleground = VariantUtils.ConvertTo<NinePatchRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._showHideTween)
		{
			_showHideTween = VariantUtils.ConvertTo<Tween>(ref value);
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
		if ((ref name) == PropertyName._foregroundTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _foregroundTween);
			return true;
		}
		if ((ref name) == PropertyName._hpForeground)
		{
			value = VariantUtils.CreateFrom<NinePatchRect>(ref _hpForeground);
			return true;
		}
		if ((ref name) == PropertyName._hpMiddleground)
		{
			value = VariantUtils.CreateFrom<NinePatchRect>(ref _hpMiddleground);
			return true;
		}
		if ((ref name) == PropertyName._showHideTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _showHideTween);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._foregroundTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._hpForeground, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._hpMiddleground, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._showHideTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._foregroundTween, Variant.From<Tween>(ref _foregroundTween));
		info.AddProperty(PropertyName._hpForeground, Variant.From<NinePatchRect>(ref _hpForeground));
		info.AddProperty(PropertyName._hpMiddleground, Variant.From<NinePatchRect>(ref _hpMiddleground));
		info.AddProperty(PropertyName._showHideTween, Variant.From<Tween>(ref _showHideTween));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._foregroundTween, ref val))
		{
			_foregroundTween = ((Variant)(ref val)).As<Tween>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._hpForeground, ref val2))
		{
			_hpForeground = ((Variant)(ref val2)).As<NinePatchRect>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._hpMiddleground, ref val3))
		{
			_hpMiddleground = ((Variant)(ref val3)).As<NinePatchRect>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._showHideTween, ref val4))
		{
			_showHideTween = ((Variant)(ref val4)).As<Tween>();
		}
	}
}
