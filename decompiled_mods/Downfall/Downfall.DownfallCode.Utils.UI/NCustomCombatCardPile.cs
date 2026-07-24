using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Utils.UI;

[ScriptPath("res://DownfallCode/Utils/UI/NCustomCombatCardPile.cs")]
public abstract class NCustomCombatCardPile : NCombatCardPile
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetAnimInOutPositions = StringName.op_Implicit("SetAnimInOutPositions");

		public static readonly StringName RefreshAnimPositions = StringName.op_Implicit("RefreshAnimPositions");

		public static readonly StringName UpdateCount = StringName.op_Implicit("UpdateCount");

		public static readonly StringName OnRelease = StringName.op_Implicit("OnRelease");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Pile = StringName.op_Implicit("Pile");

		public static readonly StringName ScenePath = StringName.op_Implicit("ScenePath");

		public static readonly StringName HideOffset = StringName.op_Implicit("HideOffset");

		public static readonly StringName HoverTipOffset = StringName.op_Implicit("HoverTipOffset");

		public static readonly StringName _ownBumpTween = StringName.op_Implicit("_ownBumpTween");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly FieldInfo PileField = typeof(NCombatCardPile).GetField("_pile", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo CurrentCountField = typeof(NCombatCardPile).GetField("_currentCount", BindingFlags.Instance | BindingFlags.NonPublic);

	private Tween? _ownBumpTween;

	private Player? _player;

	protected abstract override PileType Pile { get; }

	public abstract Func<Player, bool> CanUsePile { get; }

	public abstract string ScenePath { get; }

	protected abstract Vector2 HideOffset { get; }

	protected abstract Vector2 HoverTipOffset { get; }

	protected abstract HoverTip BuildHoverTip();

	protected abstract LocString BuildEmptyPileMessage();

	public override void _Ready()
	{
		((NClickableControl)this).ConnectSignals();
		base._emptyPileMessage = BuildEmptyPileMessage();
	}

	protected override void SetAnimInOutPositions()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		base._showPosition = ((Control)this).Position;
		base._hidePosition = ((Control)this).Position + HideOffset;
	}

	public void RefreshAnimPositions()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		base._showPosition = ((Control)this).Position;
		base._hidePosition = ((Control)this).Position + HideOffset;
	}

	public override void Initialize(Player player)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		_player = player;
		CardPile pile = PileTypeExtensions.GetPile(((NCombatCardPile)this).Pile, player);
		PileField.SetValue(this, pile);
		MegaLabel countLabel = ((Node)this).GetNode<MegaLabel>(NodePath.op_Implicit("CountContainer/Count"));
		pile.CardAddFinished += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)countLabel))
			{
				UpdateCount(pile.Cards.Count, countLabel);
			}
		};
		pile.CardRemoveFinished += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)countLabel))
			{
				UpdateCount(pile.Cards.Count, countLabel);
			}
		};
		UpdateCount(pile.Cards.Count, countLabel);
	}

	private void UpdateCount(int count, MegaLabel label)
	{
		CurrentCountField.SetValue(this, count);
		label.SetTextAutoSize(count.ToString());
	}

	protected override void OnRelease()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		((NCombatCardPile)this).OnRelease();
		if (_player == null || !CombatManager.Instance.IsInProgress)
		{
			return;
		}
		CardPile pile = PileTypeExtensions.GetPile(((NCombatCardPile)this).Pile, _player);
		if (pile.IsEmpty)
		{
			NCapstoneContainer instance = NCapstoneContainer.Instance;
			if (instance != null && instance.InUse)
			{
				NCapstoneContainer.Instance.Close();
			}
			NThoughtBubbleVfx val = NThoughtBubbleVfx.Create(base._emptyPileMessage.GetFormattedText(), _player.Creature, (double?)2.0);
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)val);
			}
		}
		else
		{
			NCapstoneContainer instance3 = NCapstoneContainer.Instance;
			ICapstoneScreen obj = ((instance3 != null) ? instance3.CurrentCapstoneScreen : null);
			NCardPileScreen val2 = (NCardPileScreen)(object)((obj is NCardPileScreen) ? obj : null);
			if (val2 != null && val2.Pile == pile)
			{
				NCapstoneContainer.Instance.Close();
			}
			else
			{
				NCardPileScreen.ShowScreen(pile, ((NButton)this).Hotkeys);
			}
		}
	}

	protected override void OnFocus()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipSet.Remove((Control)(object)this);
		NHoverTipSet val = NHoverTipSet.CreateAndShow((Control)(object)this, (IHoverTip)(object)BuildHoverTip(), (HoverTipAlignment)0);
		if (val != null)
		{
			((Control)val).GlobalPosition = ((Control)this).GlobalPosition + HoverTipOffset;
		}
		Tween? ownBumpTween = _ownBumpTween;
		if (ownBumpTween != null)
		{
			ownBumpTween.Kill();
		}
		_ownBumpTween = ((Node)this).CreateTween();
		_ownBumpTween.TweenProperty((GodotObject)(object)((Node)this).GetNode<Control>(NodePath.op_Implicit("Icon")), NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One * 1.25f), 0.05);
	}

	protected override void OnUnfocus()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipSet.Remove((Control)(object)this);
		Tween? ownBumpTween = _ownBumpTween;
		if (ownBumpTween != null)
		{
			ownBumpTween.Kill();
		}
		_ownBumpTween = ((Node)this).CreateTween().SetParallel(true);
		_ownBumpTween.TweenProperty((GodotObject)(object)((Node)this).GetNode<Control>(NodePath.op_Implicit("Icon")), NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), 0.5).SetEase((EaseType)1).SetTrans((TransitionType)5);
		_ownBumpTween.TweenProperty((GodotObject)(object)((Node)this).GetNode<Control>(NodePath.op_Implicit("Icon")), NodePath.op_Implicit("modulate"), Variant.op_Implicit(Colors.White), 0.5).SetEase((EaseType)1).SetTrans((TransitionType)5);
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
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetAnimInOutPositions, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshAnimPositions, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateCount, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("count"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)24, StringName.op_Implicit("label"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Label"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnRelease, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetAnimInOutPositions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NCombatCardPile)this).SetAnimInOutPositions();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshAnimPositions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshAnimPositions();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateCount && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			UpdateCount(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<MegaLabel>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnRelease && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnRelease();
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
		return ((NCombatCardPile)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetAnimInOutPositions)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshAnimPositions)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateCount)
		{
			return true;
		}
		if ((ref method) == MethodName.OnRelease)
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
		return ((NCombatCardPile)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._ownBumpTween)
		{
			_ownBumpTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		return ((NCombatCardPile)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Pile)
		{
			PileType pile = ((NCombatCardPile)this).Pile;
			value = VariantUtils.CreateFrom<PileType>(ref pile);
			return true;
		}
		if ((ref name) == PropertyName.ScenePath)
		{
			string scenePath = ScenePath;
			value = VariantUtils.CreateFrom<string>(ref scenePath);
			return true;
		}
		if ((ref name) == PropertyName.HideOffset)
		{
			Vector2 hideOffset = HideOffset;
			value = VariantUtils.CreateFrom<Vector2>(ref hideOffset);
			return true;
		}
		if ((ref name) == PropertyName.HoverTipOffset)
		{
			Vector2 hideOffset = HoverTipOffset;
			value = VariantUtils.CreateFrom<Vector2>(ref hideOffset);
			return true;
		}
		if ((ref name) == PropertyName._ownBumpTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _ownBumpTween);
			return true;
		}
		return ((NCombatCardPile)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._ownBumpTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.Pile, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.ScenePath, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.HideOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.HoverTipOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((NCombatCardPile)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._ownBumpTween, Variant.From<Tween>(ref _ownBumpTween));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCombatCardPile)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._ownBumpTween, ref val))
		{
			_ownBumpTween = ((Variant)(ref val)).As<Tween>();
		}
	}
}
