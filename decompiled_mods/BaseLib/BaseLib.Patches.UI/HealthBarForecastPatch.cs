using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Hooks;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
public static class HealthBarForecastPatch
{
	private sealed class HealthBarForecastUiState(Control rightContainer, Control leftContainer, NinePatchRect rightTemplate, NinePatchRect leftTemplate, List<NinePatchRect> rightSegments)
	{
		public Control RightContainer { get; } = rightContainer;

		public Control LeftContainer { get; } = leftContainer;

		public NinePatchRect RightTemplate { get; } = rightTemplate;

		public NinePatchRect LeftTemplate { get; } = leftTemplate;

		public List<NinePatchRect> RightSegments { get; } = rightSegments;

		public List<NinePatchRect> LeftSegments { get; } = new List<NinePatchRect>();

		public List<(CustomSegment Segment, int DrawIndex)> OverlapLeftZ { get; } = new List<(CustomSegment, int)>();

		public HealthBarForecastRenderResult LastRender { get; set; } = HealthBarForecastRenderResult.Empty;

		public float? MiddlegroundTweenTarget { get; set; }

		public int MiddlegroundHpOnLastTween { get; set; } = -1;

		public int MiddlegroundMaxHpOnLastTween { get; set; } = -1;
	}

	private readonly record struct CustomSegment(int Amount, Color Color, HealthBarForecastDirection Direction, int Order, long SequenceOrder, Material? OverlayMaterial, Color? OverlaySelfModulate, HealthBarForecastLeftOriginLayout LeftOriginLayout, int LeftExclusiveZGroup, bool AffectsHpLabel);

	private readonly record struct LethalCandidate(int Amount, Color? Color, int Order, long SequenceOrder);

	private readonly record struct HealthBarForecastRenderResult(bool HasRightForecast, float RightForecastEdgeOffsetRight, Color? LethalRightColor, Color? LethalLeftColor, int RemainingHpAfterRight)
	{
		public static HealthBarForecastRenderResult Empty => new HealthBarForecastRenderResult(HasRightForecast: false, 0f, null, null, 0);
	}

	private static readonly SpireField<NHealthBar, HealthBarForecastUiState?> UiStates = new SpireField<NHealthBar, HealthBarForecastUiState>(() => (HealthBarForecastUiState?)null);

	private static readonly Color DoomLethalTextColor = new Color("FB8DFF");

	private static readonly Color DoomLethalOutlineColor = new Color("2D1263");

	[ThreadStatic]
	private static bool _isRefreshingOverlay;

	[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
	[HarmonyPostfix]
	private static void RefreshForegroundPostfix(NHealthBar __instance)
	{
		RunOverlayRefresh(__instance);
	}

	[HarmonyPatch(typeof(NHealthBar), "SetHpBarContainerSizeWithOffsetsImmediately")]
	[HarmonyPostfix]
	private static void SetHpBarContainerSizeWithOffsetsImmediatelyPostfix(NHealthBar __instance)
	{
		if (__instance._creature != null && RunOverlayRefresh(__instance))
		{
			SnapMiddlegroundToForecast(__instance);
		}
	}

	[HarmonyPatch(typeof(NHealthBar), "RefreshMiddleground")]
	[HarmonyPostfix]
	private static void RefreshMiddlegroundPostfix(NHealthBar __instance)
	{
		RefreshMiddlegroundOverlay(__instance);
	}

	[HarmonyPatch(typeof(NHealthBar), "RefreshText")]
	[HarmonyPostfix]
	private static void RefreshTextPostfix(NHealthBar __instance)
	{
		RefreshTextOverlay(__instance);
	}

	private static bool RunOverlayRefresh(NHealthBar healthBar)
	{
		if (_isRefreshingOverlay)
		{
			return false;
		}
		_isRefreshingOverlay = true;
		try
		{
			RefreshForegroundOverlay(healthBar);
			return true;
		}
		finally
		{
			_isRefreshingOverlay = false;
		}
	}

	private static void RefreshForegroundOverlay(NHealthBar healthBar)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		Creature creature = healthBar._creature;
		if (creature.CurrentHp <= 0 || HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			HideAllCustomSegments(healthBar);
			return;
		}
		CustomSegment[] customSegments = GetCustomSegments(creature);
		if (customSegments.Length == 0)
		{
			HideAllCustomSegments(healthBar);
		}
		else
		{
			if (!EnsureUiState(healthBar))
			{
				return;
			}
			HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
			if (healthBarForecastUiState == null)
			{
				return;
			}
			EnsureOverlayOrder(healthBar, healthBarForecastUiState);
			float maxFgWidth = GetMaxFgWidth(healthBar);
			int maxHp = creature.MaxHp;
			Control hpForeground = healthBar._hpForeground;
			PoisonPower power = creature.GetPower<PoisonPower>();
			int num = Math.Max(0, (power != null) ? power.CalculateTotalDamageNextTurn() : 0);
			int num2 = Math.Max(0, creature.CurrentHp - num);
			CustomSegment[] array = (from segment in customSegments
				where segment.Direction == HealthBarForecastDirection.FromRight
				orderby segment.Order, segment.SequenceOrder
				select segment).ToArray();
			int num3 = num2;
			float offsetRight = hpForeground.OffsetRight;
			Color? lethalRightColor = null;
			int num4 = 0;
			CustomSegment[] array2 = array;
			for (int num5 = 0; num5 < array2.Length; num5++)
			{
				CustomSegment customSegment = array2[num5];
				if (num3 <= 0)
				{
					break;
				}
				int num6 = Math.Min(customSegment.Amount, num3);
				if (num6 > 0)
				{
					EnsureSegmentCount(healthBarForecastUiState.RightSegments, healthBarForecastUiState.RightContainer, num4 + 1, healthBarForecastUiState.RightTemplate);
					NinePatchRect val = healthBarForecastUiState.RightSegments[num4];
					int amount = num3;
					num3 -= num6;
					float fgWidth = GetFgWidth(healthBar, num3, maxHp);
					float fgWidth2 = GetFgWidth(healthBar, amount, maxHp);
					((CanvasItem)val).Visible = true;
					ApplyForecastSegmentAppearance(val, customSegment.Color, customSegment.OverlayMaterial, customSegment.OverlaySelfModulate);
					((Control)val).OffsetLeft = ((num3 > 0) ? Math.Max(0f, fgWidth - (float)val.PatchMarginLeft) : 0f);
					((Control)val).OffsetRight = fgWidth2 - maxFgWidth;
					if (num4 == 0)
					{
						offsetRight = ((Control)val).OffsetRight;
					}
					if (num3 <= 0)
					{
						lethalRightColor = (customSegment.AffectsHpLabel ? new Color?(customSegment.Color) : ((Color?)null));
					}
					num4++;
				}
			}
			HideSegments(healthBarForecastUiState.RightSegments, num4);
			if (num4 > 0)
			{
				if (num3 > 0)
				{
					((CanvasItem)hpForeground).Visible = true;
					hpForeground.OffsetRight = GetFgWidth(healthBar, num3, maxHp) - maxFgWidth;
				}
				else
				{
					((CanvasItem)hpForeground).Visible = false;
				}
				Control doomForeground = healthBar._doomForeground;
				if (((CanvasItem)doomForeground).Visible)
				{
					if (num3 > 0)
					{
						doomForeground.OffsetRight = Math.Min(doomForeground.OffsetRight, hpForeground.OffsetRight);
					}
					else
					{
						((CanvasItem)doomForeground).Visible = false;
					}
				}
			}
			if (num3 <= 0)
			{
				HideSegments(healthBarForecastUiState.LeftSegments);
				healthBarForecastUiState.OverlapLeftZ.Clear();
				healthBarForecastUiState.LastRender = new HealthBarForecastRenderResult(HasRightForecast: true, offsetRight, lethalRightColor, null, 0);
				return;
			}
			CustomSegment[] array3 = (from segment in customSegments
				where segment.Direction == HealthBarForecastDirection.FromLeft
				orderby segment.Order, segment.SequenceOrder
				select segment).ToArray();
			healthBarForecastUiState.OverlapLeftZ.Clear();
			int leftIndex = 0;
			CustomSegment[] chainedOrdered = array3.Where((CustomSegment segment) => segment.LeftOriginLayout == HealthBarForecastLeftOriginLayout.Chained).ToArray();
			PlaceChainedLeftSegments(healthBar, healthBarForecastUiState, chainedOrdered, num3, maxFgWidth, num4, offsetRight, maxHp, ref leftIndex);
			CustomSegment[] overlapSegments = array3.Where((CustomSegment segment) => segment.LeftOriginLayout == HealthBarForecastLeftOriginLayout.OverlapFromOrigin).ToArray();
			PlaceOverlapLeftSegments(healthBar, healthBarForecastUiState, overlapSegments, num3, maxFgWidth, num4, offsetRight, maxHp, ref leftIndex);
			HideSegments(healthBarForecastUiState.LeftSegments, leftIndex);
			Color? lethalLeftColor = ResolveLeftLethalColor(creature, num3, array3, healthBarForecastUiState.OverlapLeftZ);
			healthBarForecastUiState.LastRender = new HealthBarForecastRenderResult(num4 > 0, offsetRight, lethalRightColor, lethalLeftColor, num3);
		}
	}

	private static void RefreshMiddlegroundOverlay(NHealthBar healthBar)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null)
		{
			return;
		}
		if (!healthBarForecastUiState.LastRender.HasRightForecast)
		{
			healthBarForecastUiState.MiddlegroundTweenTarget = null;
			return;
		}
		Creature creature = healthBar._creature;
		if (creature.CurrentHp <= 0 || HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			return;
		}
		Control hpMiddleground = healthBar._hpMiddleground;
		float rightForecastEdgeOffsetRight = healthBarForecastUiState.LastRender.RightForecastEdgeOffsetRight;
		bool num = creature.CurrentHp != healthBarForecastUiState.MiddlegroundHpOnLastTween || creature.MaxHp != healthBarForecastUiState.MiddlegroundMaxHpOnLastTween;
		float? middlegroundTweenTarget = healthBarForecastUiState.MiddlegroundTweenTarget;
		int num2;
		if (middlegroundTweenTarget.HasValue)
		{
			float valueOrDefault = middlegroundTweenTarget.GetValueOrDefault();
			num2 = ((!Mathf.IsEqualApprox(valueOrDefault, rightForecastEdgeOffsetRight)) ? 1 : 0);
		}
		else
		{
			num2 = 1;
		}
		bool flag = (byte)num2 != 0;
		if (num || flag)
		{
			healthBarForecastUiState.MiddlegroundHpOnLastTween = creature.CurrentHp;
			healthBarForecastUiState.MiddlegroundMaxHpOnLastTween = creature.MaxHp;
			healthBarForecastUiState.MiddlegroundTweenTarget = rightForecastEdgeOffsetRight;
			bool flag2 = rightForecastEdgeOffsetRight >= hpMiddleground.OffsetRight;
			hpMiddleground.OffsetRight += 1f;
			Tween middlegroundTween = healthBar._middlegroundTween;
			if (middlegroundTween != null)
			{
				middlegroundTween.Kill();
			}
			Tween val = ((Node)healthBar).CreateTween();
			val.TweenProperty((GodotObject)(object)hpMiddleground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(rightForecastEdgeOffsetRight - 2f), 1.0).SetDelay(flag2 ? 0.0 : 1.0).SetEase((EaseType)1)
				.SetTrans((TransitionType)5);
			healthBar._middlegroundTween = val;
		}
	}

	private static void SnapMiddlegroundToForecast(NHealthBar healthBar)
	{
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null || !healthBarForecastUiState.LastRender.HasRightForecast)
		{
			return;
		}
		Creature creature = healthBar._creature;
		if (creature.CurrentHp > 0 && !BetaMainCompatibility.Creature_.ShowsInfiniteHp(creature))
		{
			float rightForecastEdgeOffsetRight = healthBarForecastUiState.LastRender.RightForecastEdgeOffsetRight;
			Tween middlegroundTween = healthBar._middlegroundTween;
			if (middlegroundTween != null)
			{
				middlegroundTween.Kill();
			}
			healthBar._hpMiddleground.OffsetRight = rightForecastEdgeOffsetRight - 2f;
			healthBarForecastUiState.MiddlegroundHpOnLastTween = creature.CurrentHp;
			healthBarForecastUiState.MiddlegroundMaxHpOnLastTween = creature.MaxHp;
			healthBarForecastUiState.MiddlegroundTweenTarget = rightForecastEdgeOffsetRight;
		}
	}

	private static void RefreshTextOverlay(NHealthBar healthBar)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null)
		{
			return;
		}
		Creature creature = healthBar._creature;
		if (creature.CurrentHp <= 0 || HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			return;
		}
		Color? val = healthBarForecastUiState.LastRender.LethalRightColor ?? healthBarForecastUiState.LastRender.LethalLeftColor;
		MegaLabel hpLabel = healthBar._hpLabel;
		if (!val.HasValue)
		{
			if (IsDoomLethalAfterRight(healthBar, creature))
			{
				((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), DoomLethalTextColor);
				((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), DoomLethalOutlineColor);
			}
		}
		else
		{
			((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), val.Value);
			((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), DarkenForOutline(val.Value));
		}
	}

	private static void PlaceChainedLeftSegments(NHealthBar healthBar, HealthBarForecastUiState state, CustomSegment[] chainedOrdered, int remainingHp, float maxWidth, int rightIndex, float rightForecastEdgeOffsetRight, int visualDenom, ref int leftIndex)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		for (int i = 0; i < chainedOrdered.Length; i++)
		{
			CustomSegment customSegment = chainedOrdered[i];
			if (num >= remainingHp)
			{
				break;
			}
			int num2 = num;
			num = Math.Min(remainingHp, num + customSegment.Amount);
			if (num > num2)
			{
				EnsureSegmentCount(state.LeftSegments, state.LeftContainer, leftIndex + 1, state.LeftTemplate);
				NinePatchRect val = state.LeftSegments[leftIndex];
				float fgWidth = GetFgWidth(healthBar, num2, visualDenom);
				float fgWidth2 = GetFgWidth(healthBar, num, visualDenom);
				((CanvasItem)val).Visible = true;
				ApplyForecastSegmentAppearance(val, customSegment.Color, customSegment.OverlayMaterial, customSegment.OverlaySelfModulate);
				((Control)val).OffsetLeft = ((num2 > 0) ? Math.Max(0f, fgWidth - (float)val.PatchMarginLeft) : 0f);
				float num3 = Math.Min(0f, fgWidth2 - maxWidth + (float)val.PatchMarginRight);
				if (rightIndex > 0)
				{
					num3 = Math.Min(num3, rightForecastEdgeOffsetRight);
				}
				((Control)val).OffsetRight = num3;
				leftIndex++;
			}
		}
	}

	private static void PlaceOverlapLeftSegments(NHealthBar healthBar, HealthBarForecastUiState state, CustomSegment[] overlapSegments, int remainingHp, float maxWidth, int rightIndex, float rightForecastEdgeOffsetRight, int visualDenom, ref int leftIndex)
	{
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		if (overlapSegments.Length == 0)
		{
			return;
		}
		foreach (IGrouping<int, CustomSegment> item2 in from segment in overlapSegments
			group segment by segment.LeftExclusiveZGroup into @group
			orderby @group.Key
			select @group)
		{
			CustomSegment[] array = (from segment in item2
				orderby segment.Amount descending, segment.Order, segment.SequenceOrder
				select segment).ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				CustomSegment item = array[num];
				int num2 = Math.Min(item.Amount, remainingHp);
				if (num2 > 0)
				{
					EnsureSegmentCount(state.LeftSegments, state.LeftContainer, leftIndex + 1, state.LeftTemplate);
					NinePatchRect val = state.LeftSegments[leftIndex];
					float fgWidth = GetFgWidth(healthBar, num2, visualDenom);
					state.OverlapLeftZ.Add((item, leftIndex));
					((CanvasItem)val).Visible = true;
					ApplyForecastSegmentAppearance(val, item.Color, item.OverlayMaterial, item.OverlaySelfModulate);
					((Control)val).OffsetLeft = 0f;
					float num3 = Math.Min(0f, fgWidth - maxWidth + (float)val.PatchMarginRight);
					if (rightIndex > 0)
					{
						num3 = Math.Min(num3, rightForecastEdgeOffsetRight);
					}
					((Control)val).OffsetRight = num3;
					leftIndex++;
				}
			}
		}
	}

	private static CustomSegment[] GetCustomSegments(Creature creature)
	{
		return (from registered in HealthBarForecastRegistry.GetSegments(creature)
			select new CustomSegment(registered.Segment.Amount, registered.Segment.Color, registered.Segment.Direction, registered.Segment.Order, registered.SequenceOrder, registered.Segment.OverlayMaterial, registered.Segment.OverlaySelfModulate, registered.Segment.LeftOriginLayout, registered.Segment.LeftExclusiveZGroup, registered.Segment.AffectsHpLabel) into segment
			where segment.Amount > 0
			select segment).ToArray();
	}

	private static void HideAllCustomSegments(NHealthBar healthBar)
	{
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState != null)
		{
			HideSegments(healthBarForecastUiState.RightSegments);
			HideSegments(healthBarForecastUiState.LeftSegments);
			healthBarForecastUiState.OverlapLeftZ.Clear();
			healthBarForecastUiState.LastRender = HealthBarForecastRenderResult.Empty;
		}
	}

	private static bool EnsureUiState(NHealthBar healthBar)
	{
		if (UiStates[healthBar] != null)
		{
			return true;
		}
		Control poisonForeground = healthBar._poisonForeground;
		NinePatchRect val = (NinePatchRect)(object)((poisonForeground is NinePatchRect) ? poisonForeground : null);
		if (val == null)
		{
			return false;
		}
		Control doomForeground = healthBar._doomForeground;
		NinePatchRect val2 = (NinePatchRect)(object)((doomForeground is NinePatchRect) ? doomForeground : null);
		if (val2 == null)
		{
			return false;
		}
		Node parent = ((Node)val).GetParent();
		Control val3 = (Control)(object)((parent is Control) ? parent : null);
		if (val3 == null)
		{
			return false;
		}
		Control val4 = CreateContainer("BaseLibForecastRightContainer");
		Control val5 = CreateContainer("BaseLibForecastLeftContainer");
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)val5, false, (InternalMode)0);
		NinePatchRect val6 = CreateSegmentTemplate(val, "BaseLibForecastRightTemplate");
		NinePatchRect val7 = CreateSegmentTemplate(val2, "BaseLibForecastLeftTemplate");
		((Node)val4).AddChild((Node)(object)val6, false, (InternalMode)0);
		((Node)val5).AddChild((Node)(object)val7, false, (InternalMode)0);
		UiStates[healthBar] = new HealthBarForecastUiState(val4, val5, val6, val7, new List<NinePatchRect>());
		return true;
	}

	private static Control CreateContainer(string name)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		Control val = new Control
		{
			Name = StringName.op_Implicit(name),
			MouseFilter = (MouseFilterEnum)2
		};
		val.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		return val;
	}

	private static NinePatchRect CreateSegmentTemplate(NinePatchRect template, string name)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		NinePatchRect val = (NinePatchRect)((Node)template).Duplicate(15);
		((Node)val).Name = StringName.op_Implicit(name);
		((CanvasItem)val).Visible = false;
		((CanvasItem)val).Modulate = Colors.White;
		((CanvasItem)val).SelfModulate = Colors.White;
		((CanvasItem)val).Material = null;
		((CanvasItem)val).ZIndex = 0;
		((Control)val).MouseFilter = (MouseFilterEnum)2;
		((Control)val).OffsetLeft = 0f;
		((Control)val).OffsetRight = 0f;
		return val;
	}

	private static void EnsureOverlayOrder(NHealthBar healthBar, HealthBarForecastUiState state)
	{
		Control poisonForeground = healthBar._poisonForeground;
		if (poisonForeground == null)
		{
			return;
		}
		Control hpForeground = healthBar._hpForeground;
		if (hpForeground == null)
		{
			return;
		}
		Control doomForeground = healthBar._doomForeground;
		if (doomForeground == null)
		{
			return;
		}
		Node parent = ((Node)poisonForeground).GetParent();
		Control val = (Control)(object)((parent is Control) ? parent : null);
		if (val != null)
		{
			if (((Node)poisonForeground).GetIndex(false) < ((Node)hpForeground).GetIndex(false))
			{
				MoveChildAfter(val, state.RightContainer, poisonForeground);
			}
			else
			{
				MoveChildBefore(val, state.RightContainer, hpForeground);
			}
			MoveChildBefore(val, state.LeftContainer, doomForeground);
		}
	}

	private static void MoveChildAfter(Control parent, Control node, Control anchor)
	{
		if ((object)((Node)node).GetParent() == parent && (object)((Node)anchor).GetParent() == parent)
		{
			int index = ((Node)node).GetIndex(false);
			int index2 = ((Node)anchor).GetIndex(false);
			int num = ((index > index2) ? (index2 + 1) : index2);
			if (index != num)
			{
				((Node)parent).MoveChild((Node)(object)node, num);
			}
		}
	}

	private static void MoveChildBefore(Control parent, Control node, Control anchor)
	{
		if ((object)((Node)node).GetParent() == parent && (object)((Node)anchor).GetParent() == parent)
		{
			int index = ((Node)node).GetIndex(false);
			int index2 = ((Node)anchor).GetIndex(false);
			int num = ((index > index2) ? index2 : Math.Max(0, index2 - 1));
			if (index != num)
			{
				((Node)parent).MoveChild((Node)(object)node, num);
			}
		}
	}

	private static void EnsureSegmentCount(List<NinePatchRect> segments, Control container, int requiredCount, NinePatchRect template)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		while (segments.Count < requiredCount)
		{
			NinePatchRect val = (NinePatchRect)((Node)template).Duplicate(15);
			((Node)val).Name = StringName.op_Implicit($"BaseLibForecastSegment{segments.Count}");
			((CanvasItem)val).Visible = false;
			((Node)container).AddChild((Node)(object)val, false, (InternalMode)0);
			segments.Add(val);
		}
	}

	private static void HideSegments(IEnumerable<NinePatchRect> segments, int startIndex = 0)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (NinePatchRect segment in segments)
		{
			if (num++ >= startIndex)
			{
				((CanvasItem)segment).Visible = false;
				((CanvasItem)segment).Material = null;
				((CanvasItem)segment).SelfModulate = Colors.White;
				((CanvasItem)segment).ZIndex = 0;
			}
		}
	}

	private static void ApplyForecastSegmentAppearance(NinePatchRect node, Color color, Material? overlayMaterial, Color? overlaySelfModulate)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)node).Material = overlayMaterial;
		((CanvasItem)node).SelfModulate = overlaySelfModulate ?? color;
	}

	private static float GetMaxFgWidth(NHealthBar healthBar)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		float expectedMaxFgWidth = healthBar._expectedMaxFgWidth;
		if (!(expectedMaxFgWidth > 0f))
		{
			return healthBar._hpForegroundContainer.Size.X;
		}
		return expectedMaxFgWidth;
	}

	private static float GetFgWidth(NHealthBar healthBar, int amount, int visualDenom)
	{
		Creature creature = healthBar._creature;
		if (visualDenom <= 0 || amount <= 0)
		{
			return 0f;
		}
		return Math.Max((float)amount / (float)visualDenom * GetMaxFgWidth(healthBar), (creature.CurrentHp > 0) ? 12f : 0f);
	}

	private static Color DarkenForOutline(Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		return new Color(Math.Clamp(color.R * 0.3f, 0f, 1f), Math.Clamp(color.G * 0.3f, 0f, 1f), Math.Clamp(color.B * 0.3f, 0f, 1f), 1f);
	}

	private static bool IsDoomLethalAfterRight(NHealthBar healthBar, Creature creature)
	{
		int powerAmount = creature.GetPowerAmount<DoomPower>();
		if (powerAmount <= 0)
		{
			return false;
		}
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null || !healthBarForecastUiState.LastRender.HasRightForecast)
		{
			return false;
		}
		int remainingHpAfterRight = healthBarForecastUiState.LastRender.RemainingHpAfterRight;
		if (remainingHpAfterRight > 0)
		{
			return powerAmount >= remainingHpAfterRight;
		}
		return false;
	}

	private static Color? ResolveLeftLethalColor(Creature creature, int remainingHp, IReadOnlyList<CustomSegment> leftSegments, List<(CustomSegment Segment, int DrawIndex)> overlapZ)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (remainingHp <= 0)
		{
			return null;
		}
		Color? result = null;
		bool flag = false;
		int num = int.MinValue;
		foreach (var (customSegment, num2) in overlapZ)
		{
			if (customSegment.Amount >= remainingHp && num2 >= num)
			{
				num = num2;
				flag = true;
				result = (customSegment.AffectsHpLabel ? new Color?(customSegment.Color) : ((Color?)null));
			}
		}
		if (flag)
		{
			return result;
		}
		List<LethalCandidate> list = new List<LethalCandidate>();
		list.AddRange(leftSegments.Where(delegate(CustomSegment segment)
		{
			CustomSegment customSegment2 = segment;
			return customSegment2.Amount > 0 && customSegment2.Direction == HealthBarForecastDirection.FromLeft && customSegment2.LeftOriginLayout == HealthBarForecastLeftOriginLayout.Chained;
		}).Select(delegate(CustomSegment segment)
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			CustomSegment customSegment2 = segment;
			int amount = customSegment2.Amount;
			customSegment2 = segment;
			Color? color;
			if (!customSegment2.AffectsHpLabel)
			{
				color = null;
			}
			else
			{
				customSegment2 = segment;
				color = customSegment2.Color;
			}
			customSegment2 = segment;
			int order = customSegment2.Order;
			customSegment2 = segment;
			return new LethalCandidate(amount, color, order, customSegment2.SequenceOrder);
		}));
		int powerAmount = creature.GetPowerAmount<DoomPower>();
		if (powerAmount > 0)
		{
			list.Add(new LethalCandidate(powerAmount, DoomLethalTextColor, 0, -2305843009213693952L));
		}
		if (list.Count == 0)
		{
			return null;
		}
		IOrderedEnumerable<LethalCandidate> orderedEnumerable = from candidate in list
			orderby candidate.Order, candidate.SequenceOrder
			select candidate;
		int num3 = 0;
		foreach (LethalCandidate item in orderedEnumerable)
		{
			num3 = Math.Min(remainingHp, num3 + item.Amount);
			if (num3 >= remainingHp)
			{
				return item.Color;
			}
		}
		return null;
	}
}
