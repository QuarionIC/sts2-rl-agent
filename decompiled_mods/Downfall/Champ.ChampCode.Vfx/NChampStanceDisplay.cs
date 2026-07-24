using System;
using System.Collections.Generic;
using System.ComponentModel;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Champ.ChampCode.Vfx;

[ScriptPath("res://ChampCode/Vfx/NChampStanceDisplay.cs")]
public class NChampStanceDisplay : Control
{
	private class StanceIconControl : NClickableControl
	{
		public class MethodName : MethodName
		{
			public static readonly StringName SetReticle = StringName.op_Implicit("SetReticle");

			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

			public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName _reticle = StringName.op_Implicit("_reticle");
		}

		public class SignalName : SignalName
		{
		}

		private NSelectionReticle? _reticle;

		private IHoverTip? _tip;

		private Func<IHoverTip>? _tipProvider;

		public void SetTipProvider(Func<IHoverTip> provider)
		{
			_tipProvider = provider;
		}

		public void SetReticle(NSelectionReticle reticle)
		{
			_reticle = reticle;
		}

		public override void _Ready()
		{
			((NClickableControl)this).ConnectSignals();
		}

		protected override void OnFocus()
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			NControllerManager instance = NControllerManager.Instance;
			if (instance != null && instance.IsUsingController)
			{
				NSelectionReticle? reticle = _reticle;
				if (reticle != null)
				{
					reticle.OnSelect();
				}
			}
			_tip = _tipProvider?.Invoke();
			if (_tip != null)
			{
				NHoverTipSet obj = NHoverTipSet.CreateAndShow((Control)(object)this, _tip, (HoverTipAlignment)0);
				if (obj != null)
				{
					((Control)obj).SetGlobalPosition(((Control)this).GlobalPosition + new Vector2(0f, ((Control)this).Size.Y + 20f), false);
				}
			}
		}

		protected override void OnUnfocus()
		{
			NSelectionReticle? reticle = _reticle;
			if (reticle != null)
			{
				reticle.OnDeselect();
			}
			NHoverTipSet.Remove((Control)(object)this);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(4)
			{
				new MethodInfo(MethodName.SetReticle, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("reticle"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.SetReticle && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				SetReticle(VariantUtils.ConvertTo<NSelectionReticle>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
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
			if ((ref method) == MethodName.SetReticle)
			{
				return true;
			}
			if ((ref method) == MethodName._Ready)
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
			if ((ref name) == PropertyName._reticle)
			{
				_reticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
				return true;
			}
			return ((NClickableControl)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName._reticle)
			{
				value = VariantUtils.CreateFrom<NSelectionReticle>(ref _reticle);
				return true;
			}
			return ((NClickableControl)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, PropertyName._reticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			((NClickableControl)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName._reticle, Variant.From<NSelectionReticle>(ref _reticle));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((NClickableControl)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName._reticle, ref val))
			{
				_reticle = ((Variant)(ref val)).As<NSelectionReticle>();
			}
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName Reposition = StringName.op_Implicit("Reposition");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _bounds = StringName.op_Implicit("_bounds");

		public static readonly StringName _creatureHitbox = StringName.op_Implicit("_creatureHitbox");
	}

	public class SignalName : SignalName
	{
	}

	private const string InactiveChargePath = "res://Champ/images/ui/stance_charge_inactive.png";

	private const int ChargeIconSize = 70;

	private const int Separation = 6;

	private const int IconCount = 3;

	private const int TotalWidth = 222;

	private const int TotalHeight = 70;

	private const int MarginAboveHead = 20;

	private static readonly Vector2 ReticleCenterOffset = new Vector2(35f, 35f);

	private static readonly Vector2 ReticleVisualSize = new Vector2(70f, 70f);

	private readonly List<TextureRect> _icons = new List<TextureRect>();

	private readonly List<StanceIconControl> _wrappers = new List<StanceIconControl>();

	private Control? _bounds;

	private Control? _creatureHitbox;

	private Player? _trackedPlayer;

	public static NChampStanceDisplay? Show(Player player)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
		if (val == null)
		{
			return null;
		}
		NChampStanceDisplay obj = new NChampStanceDisplay
		{
			_trackedPlayer = player,
			_bounds = val.Visuals.Bounds
		};
		((CanvasItem)obj).ZIndex = ((CanvasItem)val).ZIndex - 1;
		obj._creatureHitbox = ((val != null) ? val.Hitbox : null);
		NChampStanceDisplay nChampStanceDisplay = obj;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)nChampStanceDisplay);
		}
		return nChampStanceDisplay;
	}

	public override void _Ready()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).Size = new Vector2(222f, 70f);
		for (int i = 0; i < 3; i++)
		{
			TextureRect val = new TextureRect
			{
				StretchMode = (StretchModeEnum)4,
				Size = new Vector2(70f, 70f),
				MouseFilter = (MouseFilterEnum)2
			};
			StanceIconControl stanceIconControl = new StanceIconControl();
			((Control)stanceIconControl).Size = new Vector2(70f, 70f);
			((Control)stanceIconControl).Position = new Vector2((float)(i * 76), 0f);
			((Control)stanceIconControl).MouseFilter = (MouseFilterEnum)0;
			StanceIconControl stanceIconControl2 = stanceIconControl;
			((Node)stanceIconControl2).AddChild((Node)(object)val, false, (InternalMode)0);
			((Node)this).AddChild((Node)(object)stanceIconControl2, false, (InternalMode)0);
			_icons.Add(val);
			_wrappers.Add(stanceIconControl2);
			NSelectionReticle reticle = DownfallControllerNav.AttachFocusReticle((Node)(object)stanceIconControl2, ReticleCenterOffset, ReticleVisualSize, 4f);
			stanceIconControl2.SetReticle(reticle);
		}
		DownfallControllerNav.WireChain((IReadOnlyList<Control>)_wrappers, wrap: true);
		if (_creatureHitbox != null)
		{
			DownfallControllerNav.LinkAbove((IReadOnlyList<Control>)_wrappers, _creatureHitbox, _wrappers.Count - 1);
		}
		Reposition();
		Refresh();
	}

	private void Reposition()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (_bounds != null)
		{
			((Control)this).GlobalPosition = new Vector2(_bounds.GlobalPosition.X + _bounds.Size.X / 2f - 111f, _bounds.GlobalPosition.Y - 70f - 20f);
		}
	}

	public void Refresh()
	{
		if (!GodotObject.IsInstanceValid((GodotObject)(object)this) || ((GodotObject)this).IsQueuedForDeletion() || _trackedPlayer == null || _icons.Count == 0)
		{
			return;
		}
		ChampStanceModel stance = _trackedPlayer.ChampStance();
		if ((stance is ChampNoStance || stance == null) ? true : false)
		{
			((Node)this).QueueFree();
			return;
		}
		string text = stance.ChargeIconPath ?? "res://Champ/images/ui/stance_charge_inactive.png";
		for (int i = 0; i < _icons.Count; i++)
		{
			bool flag = i < stance.Charges;
			_icons[i].Texture = ResourceLoader.Load<Texture2D>(flag ? text : "res://Champ/images/ui/stance_charge_inactive.png", (string)null, (CacheMode)1);
			_wrappers[i].SetTipProvider(() => stance.HoverTip);
		}
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
			new MethodInfo(MethodName.Reposition, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
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
		if ((ref method) == MethodName.Reposition && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Reposition();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Refresh && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Refresh();
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
		if ((ref method) == MethodName.Reposition)
		{
			return true;
		}
		if ((ref method) == MethodName.Refresh)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._bounds)
		{
			_bounds = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			_creatureHitbox = VariantUtils.ConvertTo<Control>(ref value);
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
		if ((ref name) == PropertyName._bounds)
		{
			value = VariantUtils.CreateFrom<Control>(ref _bounds);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			value = VariantUtils.CreateFrom<Control>(ref _creatureHitbox);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._bounds, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._creatureHitbox, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._bounds, Variant.From<Control>(ref _bounds));
		info.AddProperty(PropertyName._creatureHitbox, Variant.From<Control>(ref _creatureHitbox));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._bounds, ref val))
		{
			_bounds = ((Variant)(ref val)).As<Control>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._creatureHitbox, ref val2))
		{
			_creatureHitbox = ((Variant)(ref val2)).As<Control>();
		}
	}
}
