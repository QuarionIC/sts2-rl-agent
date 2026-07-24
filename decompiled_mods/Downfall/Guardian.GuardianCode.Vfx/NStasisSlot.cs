using System;
using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Nodes;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.addons.mega_text;

namespace Guardian.GuardianCode.Vfx;

[GlobalClass]
[ScriptPath("res://GuardianCode/Vfx/NStasisSlot.cs")]
public class NStasisSlot : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetCard = StringName.op_Implicit("SetCard");

		public static readonly StringName ClearCard = StringName.op_Implicit("ClearCard");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName CardAnchorGlobal = StringName.op_Implicit("CardAnchorGlobal");

		public static readonly StringName _baseY = StringName.op_Implicit("_baseY");

		public static readonly StringName _count = StringName.op_Implicit("_count");

		public static readonly StringName _holder = StringName.op_Implicit("_holder");

		public static readonly StringName _visualParent = StringName.op_Implicit("_visualParent");
	}

	public class SignalName : SignalName
	{
	}

	private float _baseY;

	private MegaLabel? _count;

	private NCustomCardHolder? _holder;

	private Control? _visualParent;

	public Vector2 CardAnchorGlobal
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			Vector2 origin = ((CanvasItem)this).GetGlobalTransform().Origin;
			Vector2 size = ((Control)this).Size;
			Transform2D globalTransform = ((CanvasItem)this).GetGlobalTransform();
			return origin + size * ((Transform2D)(ref globalTransform)).Scale / 2f;
		}
	}

	private static float CardScale => 0.15f;

	private static float BigCardScale => 0.75f;

	public override void _Ready()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		AnimationPlayer node = ((Node)this).GetNode<AnimationPlayer>(NodePath.op_Implicit("AnimationPlayer"));
		node.Play(StringName.op_Implicit("idle"), -1.0, 1f, false);
		node.Seek((double)(float)GD.RandRange(0.0, node.CurrentAnimationLength), true);
		_visualParent = ((Node)this).GetNode<Control>(NodePath.op_Implicit("%Visuals"));
		_count = ((Node)this).GetNode<MegaLabel>(NodePath.op_Implicit("%Count"));
		((Label)_count).Text = "";
		((CanvasItem)_count).Visible = false;
		_baseY = _visualParent.Position.Y;
	}

	public NCustomCardHolder? SetCard(NCard cardNode)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		ClearCard();
		_holder = NCustomCardHolder.Create(cardNode, CardScale, BigCardScale);
		if (_holder == null)
		{
			return null;
		}
		((Node)_visualParent).AddChild((Node)(object)_holder, false, (InternalMode)0);
		if (cardNode.Model != null)
		{
			UpdateCounterDisplay(cardNode.Model);
		}
		Callable val = Callable.From((Action)delegate
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			if (_holder != null && _visualParent != null)
			{
				((Control)_holder).Position = _visualParent.Size / 2f - ((Control)_holder).Size / 2f;
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		return _holder;
	}

	public void UpdateCounterDisplay(CardModel card)
	{
		if (_count != null)
		{
			int stasisCounter = GuardianCmd.GetStasisCounter(card);
			if (stasisCounter > 0)
			{
				((Label)_count).Text = stasisCounter.ToString();
				((CanvasItem)_count).Visible = true;
			}
			else
			{
				((CanvasItem)_count).Visible = false;
			}
		}
	}

	public void ClearCard()
	{
		NCustomCardHolder? holder = _holder;
		if (holder != null)
		{
			((Node)holder).QueueFree();
		}
		_holder = null;
		if (GodotObject.IsInstanceValid((GodotObject)(object)_count))
		{
			((CanvasItem)_count).Visible = false;
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
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetCard, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("cardNode"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ClearCard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetCard && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			NCustomCardHolder nCustomCardHolder = SetCard(VariantUtils.ConvertTo<NCard>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<NCustomCardHolder>(ref nCustomCardHolder);
			return true;
		}
		if ((ref method) == MethodName.ClearCard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearCard();
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
		if ((ref method) == MethodName.SetCard)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearCard)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._baseY)
		{
			_baseY = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._count)
		{
			_count = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._holder)
		{
			_holder = VariantUtils.ConvertTo<NCustomCardHolder>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._visualParent)
		{
			_visualParent = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
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
		if ((ref name) == PropertyName.CardAnchorGlobal)
		{
			Vector2 cardAnchorGlobal = CardAnchorGlobal;
			value = VariantUtils.CreateFrom<Vector2>(ref cardAnchorGlobal);
			return true;
		}
		if ((ref name) == PropertyName._baseY)
		{
			value = VariantUtils.CreateFrom<float>(ref _baseY);
			return true;
		}
		if ((ref name) == PropertyName._count)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _count);
			return true;
		}
		if ((ref name) == PropertyName._holder)
		{
			value = VariantUtils.CreateFrom<NCustomCardHolder>(ref _holder);
			return true;
		}
		if ((ref name) == PropertyName._visualParent)
		{
			value = VariantUtils.CreateFrom<Control>(ref _visualParent);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._baseY, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._count, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._holder, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._visualParent, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.CardAnchorGlobal, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		info.AddProperty(PropertyName._baseY, Variant.From<float>(ref _baseY));
		info.AddProperty(PropertyName._count, Variant.From<MegaLabel>(ref _count));
		info.AddProperty(PropertyName._holder, Variant.From<NCustomCardHolder>(ref _holder));
		info.AddProperty(PropertyName._visualParent, Variant.From<Control>(ref _visualParent));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._baseY, ref val))
		{
			_baseY = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._count, ref val2))
		{
			_count = ((Variant)(ref val2)).As<MegaLabel>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._holder, ref val3))
		{
			_holder = ((Variant)(ref val3)).As<NCustomCardHolder>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._visualParent, ref val4))
		{
			_visualParent = ((Variant)(ref val4)).As<Control>();
		}
	}
}
