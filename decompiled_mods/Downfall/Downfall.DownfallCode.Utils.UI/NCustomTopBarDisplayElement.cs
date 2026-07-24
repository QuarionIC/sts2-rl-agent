using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Utils.UI;

[ScriptPath("res://DownfallCode/Utils/UI/NCustomTopBarDisplayElement.cs")]
public abstract class NCustomTopBarDisplayElement : NClickableControl, ITopBarElement
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName RefreshCount = StringName.op_Implicit("RefreshCount");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName IconNodePath = StringName.op_Implicit("IconNodePath");

		public static readonly StringName CountLabelNodePath = StringName.op_Implicit("CountLabelNodePath");

		public static readonly StringName _bumpTween = StringName.op_Implicit("_bumpTween");

		public static readonly StringName _countLabel = StringName.op_Implicit("_countLabel");

		public static readonly StringName _elapsedTime = StringName.op_Implicit("_elapsedTime");

		public static readonly StringName _icon = StringName.op_Implicit("_icon");

		public static readonly StringName _previousCount = StringName.op_Implicit("_previousCount");
	}

	public class SignalName : SignalName
	{
	}

	private Tween? _bumpTween;

	private MegaLabel? _countLabel;

	private float _elapsedTime;

	private Control? _icon;

	private float _previousCount;

	protected Player? Player;

	protected abstract string IconNodePath { get; }

	protected abstract string CountLabelNodePath { get; }

	public virtual void Initialize(Player player)
	{
		Player = player;
		RefreshCount();
	}

	public override void _Ready()
	{
		((NClickableControl)this).ConnectSignals();
		_icon = ((Node)this).GetNode<Control>(NodePath.op_Implicit(IconNodePath));
		_countLabel = ((Node)this).GetNodeOrNull<MegaLabel>(NodePath.op_Implicit(CountLabelNodePath));
	}

	protected abstract int? GetCount();

	public void RefreshCount()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		if (_countLabel == null)
		{
			return;
		}
		int? count = GetCount();
		if (!count.HasValue)
		{
			((CanvasItem)_countLabel).Visible = false;
			return;
		}
		((CanvasItem)_countLabel).Visible = true;
		if ((float?)count > _previousCount)
		{
			Tween? bumpTween = _bumpTween;
			if (bumpTween != null)
			{
				bumpTween.Kill();
			}
			_bumpTween = ((Node)this).CreateTween();
			_bumpTween.TweenProperty((GodotObject)(object)_countLabel, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), 0.5).From(Variant.op_Implicit(Vector2.One * 1.5f)).SetEase((EaseType)1)
				.SetTrans((TransitionType)5);
			((Control)_countLabel).PivotOffset = ((Control)_countLabel).Size * 0.5f;
		}
		_previousCount = count.Value;
		_countLabel.SetTextAutoSize(count.Value.ToString());
	}

	public override void _Process(double delta)
	{
		if (((NClickableControl)this).IsFocused)
		{
			_elapsedTime += (float)delta * 4f;
			_icon.Rotation = 0.12f * Mathf.Sin(_elapsedTime);
		}
	}

	protected override void OnFocus()
	{
		((NClickableControl)this).OnFocus();
		_elapsedTime = 0f;
	}

	protected override void OnUnfocus()
	{
		((NClickableControl)this).OnUnfocus();
		_icon.Rotation = 0f;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshCount, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshCount && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshCount();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnFocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnUnfocus();
			ret = default(godot_variant);
			return true;
		}
		return ((NClickableControl)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshCount)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFocus)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus)
		{
			return true;
		}
		return ((NClickableControl)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._bumpTween)
		{
			_bumpTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._countLabel)
		{
			_countLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._elapsedTime)
		{
			_elapsedTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._icon)
		{
			_icon = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._previousCount)
		{
			_previousCount = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((NClickableControl)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.IconNodePath)
		{
			string iconNodePath = IconNodePath;
			value = VariantUtils.CreateFrom<string>(ref iconNodePath);
			return true;
		}
		if ((ref name) == PropertyName.CountLabelNodePath)
		{
			string iconNodePath = CountLabelNodePath;
			value = VariantUtils.CreateFrom<string>(ref iconNodePath);
			return true;
		}
		if ((ref name) == PropertyName._bumpTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _bumpTween);
			return true;
		}
		if ((ref name) == PropertyName._countLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _countLabel);
			return true;
		}
		if ((ref name) == PropertyName._elapsedTime)
		{
			value = VariantUtils.CreateFrom<float>(ref _elapsedTime);
			return true;
		}
		if ((ref name) == PropertyName._icon)
		{
			value = VariantUtils.CreateFrom<Control>(ref _icon);
			return true;
		}
		if ((ref name) == PropertyName._previousCount)
		{
			value = VariantUtils.CreateFrom<float>(ref _previousCount);
			return true;
		}
		return ((NClickableControl)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._bumpTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._countLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._elapsedTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._icon, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._previousCount, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.IconNodePath, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.CountLabelNodePath, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((NClickableControl)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._bumpTween, Variant.From<Tween>(ref _bumpTween));
		info.AddProperty(PropertyName._countLabel, Variant.From<MegaLabel>(ref _countLabel));
		info.AddProperty(PropertyName._elapsedTime, Variant.From<float>(ref _elapsedTime));
		info.AddProperty(PropertyName._icon, Variant.From<Control>(ref _icon));
		info.AddProperty(PropertyName._previousCount, Variant.From<float>(ref _previousCount));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NClickableControl)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._bumpTween, ref val))
		{
			_bumpTween = ((Variant)(ref val)).As<Tween>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._countLabel, ref val2))
		{
			_countLabel = ((Variant)(ref val2)).As<MegaLabel>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._elapsedTime, ref val3))
		{
			_elapsedTime = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._icon, ref val4))
		{
			_icon = ((Variant)(ref val4)).As<Control>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._previousCount, ref val5))
		{
			_previousCount = ((Variant)(ref val5)).As<float>();
		}
	}
}
