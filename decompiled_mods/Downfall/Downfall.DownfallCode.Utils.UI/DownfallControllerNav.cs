using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Utils.UI;

public static class DownfallControllerNav
{
	private const string WiredMetaKey = "downfall_controller_nav_wired";

	private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

	private static readonly StyleBoxEmpty BlankFocusStyle = new StyleBoxEmpty();

	private static PackedScene? _reticleScene;

	private static readonly Dictionary<Control, (IReadOnlyList<Control> Controls, int EntryIndex)> AnchorLinks = new Dictionary<Control, (IReadOnlyList<Control>, int)>();

	public static void WireChain(IReadOnlyList<Control> controls, bool wrap = false, bool rtl = false)
	{
		for (int i = 0; i < controls.Count; i++)
		{
			Control val = controls[i];
			val.FocusMode = (FocusModeEnum)2;
			val.AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)(object)BlankFocusStyle);
			object obj;
			if (i <= 0)
			{
				if (!wrap || controls.Count <= 1)
				{
					obj = null;
				}
				else
				{
					obj = controls[controls.Count - 1];
				}
			}
			else
			{
				obj = controls[i - 1];
			}
			Control val2 = (Control)obj;
			Control val3 = ((i < controls.Count - 1) ? controls[i + 1] : ((wrap && controls.Count > 1) ? controls[0] : null));
			Control val4 = (rtl ? val3 : val2);
			Control val5 = (rtl ? val2 : val3);
			if (val4 != null)
			{
				val.FocusNeighborLeft = ((Node)val4).GetPath();
			}
			if (val5 != null)
			{
				val.FocusNeighborRight = ((Node)val5).GetPath();
			}
		}
	}

	public static void LinkAbove(IReadOnlyList<Control> controls, Control anchor, int entryIndex = 0)
	{
		if (controls.Count == 0)
		{
			AnchorLinks.Remove(anchor);
			return;
		}
		AnchorLinks[anchor] = (controls, entryIndex);
		ApplyAnchorLink(anchor);
	}

	public static void ReapplyAnchorLink(Control anchor)
	{
		if (!GodotObject.IsInstanceValid((GodotObject)(object)anchor))
		{
			AnchorLinks.Remove(anchor);
		}
		else
		{
			ApplyAnchorLink(anchor);
		}
	}

	private static void ApplyAnchorLink(Control anchor)
	{
		if (!AnchorLinks.TryGetValue(anchor, out (IReadOnlyList<Control>, int) value))
		{
			return;
		}
		foreach (Control item in value.Item1)
		{
			if (!GodotObject.IsInstanceValid((GodotObject)(object)item))
			{
				AnchorLinks.Remove(anchor);
				return;
			}
		}
		anchor.FocusNeighborTop = ((Node)value.Item1[value.Item2]).GetPath();
		foreach (Control item2 in value.Item1)
		{
			item2.FocusNeighborBottom = ((Node)anchor).GetPath();
		}
	}

	public static void WireHover(Control control, Action onFocus, Action onUnfocus)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		control.FocusMode = (FocusModeEnum)2;
		control.AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)(object)BlankFocusStyle);
		bool isHovered;
		bool isFocused;
		bool wasActive;
		if (!((GodotObject)control).HasMeta(StringName.op_Implicit("downfall_controller_nav_wired")))
		{
			((GodotObject)control).SetMeta(StringName.op_Implicit("downfall_controller_nav_wired"), Variant.op_Implicit(true));
			isHovered = false;
			isFocused = false;
			wasActive = false;
			((GodotObject)control).Connect(SignalName.MouseEntered, Callable.From((Action)delegate
			{
				isHovered = true;
				Refresh();
			}), 0u);
			((GodotObject)control).Connect(SignalName.MouseExited, Callable.From((Action)delegate
			{
				isHovered = false;
				Refresh();
			}), 0u);
			((GodotObject)control).Connect(SignalName.FocusEntered, Callable.From((Action)delegate
			{
				isFocused = true;
				Refresh();
			}), 0u);
			((GodotObject)control).Connect(SignalName.FocusExited, Callable.From((Action)delegate
			{
				isFocused = false;
				Refresh();
			}), 0u);
		}
		void Refresh()
		{
			bool flag = isHovered || isFocused;
			if (flag != wasActive)
			{
				wasActive = flag;
				if (flag)
				{
					onFocus();
				}
				else
				{
					onUnfocus();
				}
			}
		}
	}

	public static NSelectionReticle AttachFocusReticle(Node parent, Vector2 center, Vector2 hitboxSize, float margin = 12f)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (_reticleScene == null)
		{
			_reticleScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/selection_reticle.tscn", (string)null, (CacheMode)1);
		}
		NSelectionReticle val = _reticleScene.Instantiate<NSelectionReticle>((GenEditState)0);
		Vector2 val2 = hitboxSize / 2f + new Vector2(margin, margin);
		((Control)val).Position = center - val2;
		((Control)val).Size = val2 * 2f;
		parent.AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	public static void WireHoverChain(IReadOnlyList<Control> controls, Action<int> onFocus, Action<int> onUnfocus, bool wrap = false, bool rtl = false)
	{
		WireChain(controls, wrap, rtl);
		for (int i = 0; i < controls.Count; i++)
		{
			int index = i;
			WireHover(controls[i], delegate
			{
				onFocus(index);
			}, delegate
			{
				onUnfocus(index);
			});
		}
	}
}
