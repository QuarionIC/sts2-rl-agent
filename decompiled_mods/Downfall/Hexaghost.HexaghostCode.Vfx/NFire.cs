using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Hexaghost.HexaghostCode.Vfx;

[GlobalClass]
[ScriptPath("res://HexaghostCode/Vfx/NFire.cs")]
public class NFire : Node2D
{
	public enum FireSize
	{
		Large,
		Small
	}

	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName GetColorNode = StringName.op_Implicit("GetColorNode");

		public static readonly StringName SetState = StringName.op_Implicit("SetState");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName CurrentSize = StringName.op_Implicit("CurrentSize");

		public static readonly StringName _blue = StringName.op_Implicit("_blue");

		public static readonly StringName _currentColor = StringName.op_Implicit("_currentColor");

		public static readonly StringName _green = StringName.op_Implicit("_green");

		public static readonly StringName _orange = StringName.op_Implicit("_orange");

		public static readonly StringName _pink = StringName.op_Implicit("_pink");

		public static readonly StringName _red = StringName.op_Implicit("_red");

		public static readonly StringName _yellow = StringName.op_Implicit("_yellow");
	}

	public class SignalName : SignalName
	{
	}

	private const float LargeScale = 0.5f;

	private const float SmallScale = 0.25f;

	private Node2D? _blue;

	private FireColor _currentColor;

	private Node2D? _green;

	private Node2D? _orange;

	private Node2D? _pink;

	private Node2D? _red;

	private Node2D? _yellow;

	public FireSize CurrentSize { get; private set; } = FireSize.Small;

	public override void _Ready()
	{
		_red = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_red"));
		_green = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_green"));
		_blue = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_blue"));
		_yellow = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_yellow"));
		_pink = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_pink"));
		_orange = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit("%fire_orange"));
	}

	private Node2D? GetColorNode(FireColor color)
	{
		return (Node2D?)(color switch
		{
			FireColor.Red => _red, 
			FireColor.Green => _green, 
			FireColor.Blue => _blue, 
			FireColor.Yellow => _yellow, 
			FireColor.Pink => _pink, 
			FireColor.Orange => _orange, 
			_ => _red, 
		});
	}

	public void SetState(FireColor color, FireSize size, bool instant = false)
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		_currentColor = color;
		CurrentSize = size;
		float num = ((size == FireSize.Large) ? 0.5f : 0.25f);
		Node2D showNode = ((size == FireSize.Large && color != FireColor.Green) ? _green : GetColorNode(color));
		List<Node2D> list = (from val2 in Enum.GetValues<FireColor>().Select(GetColorNode)
			where val2 != null && val2 != showNode
			select val2).ToList();
		if (showNode == null)
		{
			return;
		}
		if (instant)
		{
			((Node2D)this).Scale = new Vector2(num, num);
			((CanvasItem)showNode).Visible = true;
			((CanvasItem)showNode).Modulate = new Color(1f, 1f, 1f, 1f);
			{
				foreach (Node2D item in list)
				{
					((CanvasItem)item).Visible = false;
					((CanvasItem)item).Modulate = new Color(1f, 1f, 1f, 1f);
				}
				return;
			}
		}
		Tween val = ((Node)this).CreateTween().SetParallel(true);
		val.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale"), Variant.op_Implicit(new Vector2(num, num)), 0.30000001192092896).SetTrans((TransitionType)1).SetEase((EaseType)1);
		((CanvasItem)showNode).Modulate = new Color(1f, 1f, 1f, (float)(((CanvasItem)showNode).Visible ? 1 : 0));
		((CanvasItem)showNode).Visible = true;
		val.TweenProperty((GodotObject)(object)showNode, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.30000001192092896);
		foreach (Node2D n in list.Where((Node2D val2) => ((CanvasItem)val2).Visible))
		{
			val.TweenProperty((GodotObject)(object)n, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.30000001192092896);
			val.Chain().TweenCallback(Callable.From<bool>((Func<bool>)(() => ((CanvasItem)n).Visible = false)));
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetColorNode, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("size"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("instant"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetColorNode && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Node2D colorNode = GetColorNode(VariantUtils.ConvertTo<FireColor>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Node2D>(ref colorNode);
			return true;
		}
		if ((ref method) == MethodName.SetState && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			SetState(VariantUtils.ConvertTo<FireColor>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<FireSize>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[2]));
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
		if ((ref method) == MethodName.GetColorNode)
		{
			return true;
		}
		if ((ref method) == MethodName.SetState)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.CurrentSize)
		{
			CurrentSize = VariantUtils.ConvertTo<FireSize>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._blue)
		{
			_blue = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentColor)
		{
			_currentColor = VariantUtils.ConvertTo<FireColor>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._green)
		{
			_green = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._orange)
		{
			_orange = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._pink)
		{
			_pink = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._red)
		{
			_red = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._yellow)
		{
			_yellow = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.CurrentSize)
		{
			FireSize currentSize = CurrentSize;
			value = VariantUtils.CreateFrom<FireSize>(ref currentSize);
			return true;
		}
		if ((ref name) == PropertyName._blue)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _blue);
			return true;
		}
		if ((ref name) == PropertyName._currentColor)
		{
			value = VariantUtils.CreateFrom<FireColor>(ref _currentColor);
			return true;
		}
		if ((ref name) == PropertyName._green)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _green);
			return true;
		}
		if ((ref name) == PropertyName._orange)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _orange);
			return true;
		}
		if ((ref name) == PropertyName._pink)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _pink);
			return true;
		}
		if ((ref name) == PropertyName._red)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _red);
			return true;
		}
		if ((ref name) == PropertyName._yellow)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _yellow);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._blue, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._currentColor, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._green, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._orange, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._pink, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._red, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._yellow, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.CurrentSize, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName currentSize = PropertyName.CurrentSize;
		FireSize currentSize2 = CurrentSize;
		info.AddProperty(currentSize, Variant.From<FireSize>(ref currentSize2));
		info.AddProperty(PropertyName._blue, Variant.From<Node2D>(ref _blue));
		info.AddProperty(PropertyName._currentColor, Variant.From<FireColor>(ref _currentColor));
		info.AddProperty(PropertyName._green, Variant.From<Node2D>(ref _green));
		info.AddProperty(PropertyName._orange, Variant.From<Node2D>(ref _orange));
		info.AddProperty(PropertyName._pink, Variant.From<Node2D>(ref _pink));
		info.AddProperty(PropertyName._red, Variant.From<Node2D>(ref _red));
		info.AddProperty(PropertyName._yellow, Variant.From<Node2D>(ref _yellow));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.CurrentSize, ref val))
		{
			CurrentSize = ((Variant)(ref val)).As<FireSize>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._blue, ref val2))
		{
			_blue = ((Variant)(ref val2)).As<Node2D>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentColor, ref val3))
		{
			_currentColor = ((Variant)(ref val3)).As<FireColor>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._green, ref val4))
		{
			_green = ((Variant)(ref val4)).As<Node2D>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._orange, ref val5))
		{
			_orange = ((Variant)(ref val5)).As<Node2D>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._pink, ref val6))
		{
			_pink = ((Variant)(ref val6)).As<Node2D>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._red, ref val7))
		{
			_red = ((Variant)(ref val7)).As<Node2D>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._yellow, ref val8))
		{
			_yellow = ((Variant)(ref val8)).As<Node2D>();
		}
	}
}
