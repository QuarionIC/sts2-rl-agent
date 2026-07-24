using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.addons.mega_text;

namespace ActsFromThePast.Minigames;

public class NPortalMapBuilderScreen : Control, IOverlayScreen, IScreenContext
{
	private class NodeSlot
	{
		public TextureRect Icon { get; init; } = null;

		public Label? LeftArrow { get; init; }

		public Label? RightArrow { get; init; }

		public int Index { get; init; }

		public bool IsLocked { get; init; }
	}

	private static readonly Color GoldColor = new Color(0.996078f, 0.819608f, 0f, 1f);

	private static readonly Color CreamColor = new Color(1f, 0.964706f, 0.886275f, 1f);

	private static readonly Color DarkPurple = new Color(0.0156863f, 0f, 0.156863f, 0.752941f);

	private static readonly Color OverBudgetColor = new Color(1f, 0.3f, 0.3f, 1f);

	private static readonly Color SelectedGlow = new Color(1f, 0.85f, 0.4f, 1f);

	private static readonly Color DimmedColor = new Color(0.85f, 0.8f, 0.75f, 1f);

	private static readonly Color PathDotColor = new Color(0.6f, 0.55f, 0.75f, 0.6f);

	private static readonly Color LockedXColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);

	private const string LocPrefix = "ACTSFROMTHEPAST-SECRET_PORTAL.minigame";

	private const float NodeSpacing = 100f;

	private const float IconSize = 72f;

	private const float ArrowOffset = 60f;

	private const int PathDotCount = 2;

	private const float PathDotSize = 6f;

	private const float LegendIconSize = 40f;

	private static NPortalMapBuilderScreen? _instance;

	private PortalMapBuilderMinigame _minigame = null;

	private readonly List<NodeSlot> _slots = new List<NodeSlot>();

	private NProceedButton? _proceedButton;

	private bool _proceedButtonReady;

	private Button? _randomizeButton;

	private MegaRichTextLabel _budgetLabel = null;

	private Tween? _fadeTween;

	private int _hoveredIndex = -1;

	private FontVariation? _fontBold;

	private FontVariation? _fontRegular;

	private readonly Dictionary<MapPointType, Texture2D?> _iconCache = new Dictionary<MapPointType, Texture2D>();

	private Texture2D? _xTexture;

	private static readonly (MapPointType type, string locKey, int cost)[] LegendEntries = new(MapPointType, string, int)[7]
	{
		((MapPointType)1, "unknown", 1),
		((MapPointType)2, "merchant", 2),
		((MapPointType)3, "treasure", 3),
		((MapPointType)4, "restSite", 3),
		((MapPointType)5, "enemy", 1),
		((MapPointType)6, "elite", 2),
		((MapPointType)0, "empty", 0)
	};

	public NetScreenType ScreenType => (NetScreenType)0;

	public bool UseSharedBackstop => false;

	public Control DefaultFocusedControl
	{
		get
		{
			foreach (NodeSlot slot in _slots)
			{
				if (!slot.IsLocked)
				{
					return (Control)(object)slot.Icon;
				}
			}
			return (Control)(((object)_proceedButton) ?? ((object)this));
		}
	}

	public static NPortalMapBuilderScreen ShowScreen(PortalMapBuilderMinigame minigame)
	{
		if (_instance != null && GodotObject.IsInstanceValid((GodotObject)(object)_instance))
		{
			((Node)_instance).QueueFree();
		}
		NPortalMapBuilderScreen nPortalMapBuilderScreen = new NPortalMapBuilderScreen();
		nPortalMapBuilderScreen._minigame = minigame;
		nPortalMapBuilderScreen.LoadResources();
		nPortalMapBuilderScreen.BuildUI();
		nPortalMapBuilderScreen.BindMinigameEvents();
		nPortalMapBuilderScreen.RefreshAll();
		_instance = nPortalMapBuilderScreen;
		NOverlayStack.Instance.Push((IOverlayScreen)(object)nPortalMapBuilderScreen);
		nPortalMapBuilderScreen.SetupNodeFocusNeighbors();
		return nPortalMapBuilderScreen;
	}

	private void BindMinigameEvents()
	{
		_minigame.SelectionChanged += OnSelectionChanged;
		_minigame.NodesChanged += OnNodesChanged;
		_minigame.Randomized += OnRandomized;
		_minigame.Finished += OnMinigameFinished;
	}

	private void UnbindMinigameEvents()
	{
		_minigame.SelectionChanged -= OnSelectionChanged;
		_minigame.NodesChanged -= OnNodesChanged;
		_minigame.Randomized -= OnRandomized;
		_minigame.Finished -= OnMinigameFinished;
	}

	private void LoadResources()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		_fontBold = GD.Load<FontVariation>("res://themes/kreon_bold_glyph_space_one.tres");
		_fontRegular = GD.Load<FontVariation>("res://themes/kreon_regular_shared.tres");
		MapPointType[] array = new MapPointType[6];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		MapPointType[] array2 = (MapPointType[])(object)array;
		foreach (MapPointType val in array2)
		{
			string iconPath = GetIconPath(val);
			Texture2D val2 = ResourceLoader.Load<Texture2D>(iconPath, (string)null, (CacheMode)1);
			if (val2 == null)
			{
				string imagePath = ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/" + GetIconName(val) + ".tres");
				val2 = ResourceLoader.Load<Texture2D>(imagePath, (string)null, (CacheMode)1);
			}
			_iconCache[val] = val2;
		}
		_xTexture = (Texture2D?)(object)GenerateXTexture(64, LockedXColor);
		_iconCache[(MapPointType)0] = _xTexture;
	}

	private static ImageTexture GenerateXTexture(int size, Color color)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		Image val = Image.CreateEmpty(size, size, false, (Format)5);
		int num = 5;
		int num2 = size / 4;
		for (int i = num2; i < size - num2; i++)
		{
			float num3 = (float)(i - num2) / (float)(size - 2 * num2 - 1);
			int num4 = num2 + (int)(num3 * (float)(size - 2 * num2 - 1));
			int num5 = size - 1 - num2 - (int)(num3 * (float)(size - 2 * num2 - 1));
			for (int j = -num; j <= num; j++)
			{
				int num6 = num4 + j;
				if (num6 >= 0 && num6 < size)
				{
					val.SetPixel(i, num6, color);
				}
				int num7 = num5 + j;
				if (num7 >= 0 && num7 < size)
				{
					val.SetPixel(i, num7, color);
				}
			}
		}
		return ImageTexture.CreateFromImage(val);
	}

	private static string GetIconPath(MapPointType type)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (type - 1) switch
		{
			4 => "res://images/atlases/ui_atlas.sprites/map/icons/map_monster.tres", 
			5 => "res://images/atlases/ui_atlas.sprites/map/icons/map_elite.tres", 
			3 => "res://images/atlases/ui_atlas.sprites/map/icons/map_rest.tres", 
			1 => "res://images/atlases/ui_atlas.sprites/map/icons/map_shop.tres", 
			2 => "res://images/atlases/ui_atlas.sprites/map/icons/map_chest.tres", 
			0 => "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres", 
			_ => "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetIconName(MapPointType type)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (type - 1) switch
		{
			4 => "map_monster", 
			5 => "map_elite", 
			3 => "map_rest", 
			1 => "map_shop", 
			2 => "map_chest", 
			0 => "map_unknown", 
			_ => "map_unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public override void _Ready()
	{
	}

	public override void _ExitTree()
	{
		UnbindMinigameEvents();
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		_minigame.ForceEnd();
		_instance = null;
	}

	private void OnSelectionChanged()
	{
		RefreshAll();
	}

	private void OnNodesChanged()
	{
		RefreshAll();
	}

	private void OnRandomized()
	{
		if (_randomizeButton != null)
		{
			((BaseButton)_randomizeButton).Disabled = true;
		}
		RefreshAll();
		NProceedButton? proceedButton = _proceedButton;
		if (proceedButton != null)
		{
			((Control)proceedButton).GrabFocus();
		}
	}

	public void AfterOverlayOpened()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		_fadeTween = ((Node)this).CreateTween();
		_fadeTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1.0), 0.5).From(Variant.op_Implicit(0f));
	}

	public void AfterOverlayClosed()
	{
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		GodotTreeExtensions.QueueFreeSafely((Node)(object)this);
	}

	public void AfterOverlayShown()
	{
		UpdateProceedButton();
	}

	public void AfterOverlayHidden()
	{
		UpdateProceedButton();
	}

	private void BuildUI()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Expected O, but got Unknown
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		ColorRect val = new ColorRect
		{
			Color = new Color(0.14f, 0.06f, 0.32f, 1f)
		};
		((Control)val).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		ColorRect val2 = new ColorRect
		{
			Color = new Color(0.18f, 0.09f, 0.4f, 0.5f)
		};
		((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
		Control val3 = new Control();
		val3.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		val3.MouseFilter = (MouseFilterEnum)2;
		((Node)this).AddChild((Node)(object)val3, false, (InternalMode)0);
		float num = (float)(_minigame.NodeCountTotal - 1) * 100f;
		float num2 = (0f - num) / 2f;
		float num3 = 0f;
		float num4 = 0.5f;
		float num5 = 0.5f;
		NinePatchRect val4 = new NinePatchRect
		{
			Modulate = new Color(1f, 0.85f, 0.5f, 0.12f)
		};
		Texture2D val5 = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
		if (val5 != null)
		{
			val4.Texture = val5;
			val4.RegionRect = new Rect2(0f, 0f, 48f, 48f);
			val4.PatchMarginLeft = 14;
			val4.PatchMarginTop = 14;
			val4.PatchMarginRight = 13;
			val4.PatchMarginBottom = 14;
		}
		((Control)val4).AnchorLeft = num4;
		((Control)val4).AnchorTop = num5;
		((Control)val4).AnchorRight = num4;
		((Control)val4).AnchorBottom = num5;
		((Control)val4).OffsetLeft = -160f;
		((Control)val4).OffsetTop = num2 - 80f;
		((Control)val4).OffsetRight = 160f;
		((Control)val4).OffsetBottom = num2 + num + 50f;
		((Control)val4).GrowHorizontal = (GrowDirection)2;
		((Control)val4).GrowVertical = (GrowDirection)2;
		((Control)val4).MouseFilter = (MouseFilterEnum)2;
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		for (int i = 0; i < _minigame.NodeCountTotal; i++)
		{
			float num6 = num2 + (float)i * 100f;
			_slots.Add(CreateNodeSlot(i, val3, num4, num5, num3, num6));
			if (i < _minigame.NodeCountTotal - 1)
			{
				CreatePathDots(val3, num4, num5, num3, num6, num3, num6 + 100f);
			}
		}
		LocString val6 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.title");
		MegaRichTextLabel val7 = CreateStyledText("[center]" + val6.GetFormattedText() + "[/center]", _fontBold, 32, GoldColor);
		((Control)val7).AnchorLeft = num4;
		((Control)val7).AnchorTop = num5;
		((Control)val7).AnchorRight = num4;
		((Control)val7).AnchorBottom = num5;
		((Control)val7).OffsetLeft = -150f;
		((Control)val7).OffsetTop = num2 - 70f;
		((Control)val7).OffsetRight = 150f;
		((Control)val7).OffsetBottom = num2 - 20f;
		((Control)val7).GrowHorizontal = (GrowDirection)2;
		((Control)val7).GrowVertical = (GrowDirection)2;
		((Node)val3).AddChild((Node)(object)val7, false, (InternalMode)0);
		BuildLegendPanel(val3);
		BuildInstructionsPanel(val3);
		_budgetLabel = CreateStyledText("", _fontBold, 32, CreamColor);
		((Control)_budgetLabel).AnchorTop = 1f;
		((Control)_budgetLabel).AnchorBottom = 1f;
		((Control)_budgetLabel).OffsetLeft = 64f;
		((Control)_budgetLabel).OffsetTop = -89f;
		((Control)_budgetLabel).OffsetRight = 770f;
		((Control)_budgetLabel).OffsetBottom = -48f;
		((Control)_budgetLabel).GrowVertical = (GrowDirection)0;
		((Control)_budgetLabel).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 12);
		((Control)_budgetLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0.15f, 0.1f, 0.23f, 1f));
		((Control)_budgetLabel).AddThemeColorOverride(StringName.op_Implicit("font_shadow_color"), new Color(0f, 0f, 0f, 0.5f));
		((Control)_budgetLabel).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_x"), 5);
		((Control)_budgetLabel).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_y"), 4);
		((Node)this).AddChild((Node)(object)_budgetLabel, false, (InternalMode)0);
		BuildProceedButton();
		BuildRandomizeButton();
	}

	private NodeSlot CreateNodeSlot(int index, Control parent, float anchorX, float anchorY, float cx, float cy)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		float num = 36f;
		int idx = index;
		bool flag = _minigame.IsLocked(index);
		TextureRect icon = new TextureRect
		{
			CustomMinimumSize = new Vector2(72f, 72f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			AnchorLeft = anchorX,
			AnchorTop = anchorY,
			AnchorRight = anchorX,
			AnchorBottom = anchorY,
			OffsetLeft = cx - num,
			OffsetTop = cy - num,
			OffsetRight = cx + num,
			OffsetBottom = cy + num,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			PivotOffset = new Vector2(num, num),
			MouseFilter = (MouseFilterEnum)(flag ? 2 : 0)
		};
		MapPointType nodeType = _minigame.GetNodeType(index);
		if (_iconCache.TryGetValue(nodeType, out Texture2D value) && value != null)
		{
			icon.Texture = value;
		}
		if (!flag)
		{
			((Control)icon).FocusMode = (FocusModeEnum)2;
			((GodotObject)icon).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)delegate(InputEvent ev)
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Invalid comparison between Unknown and I8
				InputEventMouseButton val = (InputEventMouseButton)(object)((ev is InputEventMouseButton) ? ev : null);
				if (val != null && val.Pressed && (long)val.ButtonIndex == 1)
				{
					_minigame.SelectNode(idx);
					((Control)icon).AcceptEvent();
				}
				else if (ev.IsActionPressed(MegaInput.select, false, false))
				{
					_minigame.SelectNode(idx);
					((Control)icon).AcceptEvent();
				}
				else if (ev.IsActionPressed(MegaInput.left, false, false))
				{
					if (_minigame.SelectedIndex != idx)
					{
						_minigame.SelectNode(idx);
					}
					_minigame.CycleSelectedNode(-1);
					((Control)icon).AcceptEvent();
				}
				else if (ev.IsActionPressed(MegaInput.right, false, false))
				{
					if (_minigame.SelectedIndex != idx)
					{
						_minigame.SelectNode(idx);
					}
					_minigame.CycleSelectedNode(1);
					((Control)icon).AcceptEvent();
				}
			}), 0u);
			((GodotObject)icon).Connect(SignalName.MouseEntered, Callable.From((Action)delegate
			{
				SetHovered(idx);
			}), 0u);
			((GodotObject)icon).Connect(SignalName.MouseExited, Callable.From((Action)delegate
			{
				ClearHovered(idx);
			}), 0u);
			((GodotObject)icon).Connect(SignalName.FocusEntered, Callable.From((Action)delegate
			{
				SetHovered(idx);
			}), 0u);
			((GodotObject)icon).Connect(SignalName.FocusExited, Callable.From((Action)delegate
			{
				ClearHovered(idx);
			}), 0u);
		}
		else
		{
			((CanvasItem)icon).SelfModulate = LockedXColor;
		}
		((Node)parent).AddChild((Node)(object)icon, false, (InternalMode)0);
		Label leftArrow = null;
		Label rightArrow = null;
		if (!flag)
		{
			leftArrow = CreateArrowLabel("◀", anchorX, anchorY, cx - num - 60f, cy - 16f, cx - num - 10f, cy + 16f);
			((GodotObject)leftArrow).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)delegate(InputEvent ev)
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Invalid comparison between Unknown and I8
				InputEventMouseButton val = (InputEventMouseButton)(object)((ev is InputEventMouseButton) ? ev : null);
				if (val != null && val.Pressed && (long)val.ButtonIndex == 1)
				{
					_minigame.CycleSelectedNode(-1);
					((Control)leftArrow).AcceptEvent();
				}
			}), 0u);
			((Node)parent).AddChild((Node)(object)leftArrow, false, (InternalMode)0);
			rightArrow = CreateArrowLabel("▶", anchorX, anchorY, cx + num + 10f, cy - 16f, cx + num + 60f, cy + 16f);
			((GodotObject)rightArrow).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)delegate(InputEvent ev)
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Invalid comparison between Unknown and I8
				InputEventMouseButton val = (InputEventMouseButton)(object)((ev is InputEventMouseButton) ? ev : null);
				if (val != null && val.Pressed && (long)val.ButtonIndex == 1)
				{
					_minigame.CycleSelectedNode(1);
					((Control)rightArrow).AcceptEvent();
				}
			}), 0u);
			((Node)parent).AddChild((Node)(object)rightArrow, false, (InternalMode)0);
		}
		return new NodeSlot
		{
			Icon = icon,
			LeftArrow = leftArrow,
			RightArrow = rightArrow,
			Index = index,
			IsLocked = flag
		};
	}

	private Label CreateArrowLabel(string text, float anchorX, float anchorY, float left, float top, float right, float bottom)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label
		{
			Text = text,
			HorizontalAlignment = (HorizontalAlignment)1,
			VerticalAlignment = (VerticalAlignment)1,
			Visible = false,
			MouseFilter = (MouseFilterEnum)0,
			AnchorLeft = anchorX,
			AnchorTop = anchorY,
			AnchorRight = anchorX,
			AnchorBottom = anchorY,
			OffsetLeft = left,
			OffsetTop = top,
			OffsetRight = right,
			OffsetBottom = bottom,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2
		};
		if (_fontBold != null)
		{
			((Control)val).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)_fontBold);
		}
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 28);
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), GoldColor);
		return val;
	}

	private void CreatePathDots(Control parent, float anchorX, float anchorY, float x1, float y1, float x2, float y2)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		float num = 3f;
		float num2 = 18f;
		float num3 = y1 + num2;
		float num4 = y2 - num2;
		for (int i = 1; i <= 2; i++)
		{
			float num5 = (float)i / 3f;
			float num6 = Mathf.Lerp(x1, x2, num5);
			float num7 = Mathf.Lerp(num3, num4, num5);
			((Node)parent).AddChild((Node)new ColorRect
			{
				Color = PathDotColor,
				AnchorLeft = anchorX,
				AnchorTop = anchorY,
				AnchorRight = anchorX,
				AnchorBottom = anchorY,
				OffsetLeft = num6 - num,
				OffsetTop = num7 - num,
				OffsetRight = num6 + num,
				OffsetBottom = num7 + num,
				GrowHorizontal = (GrowDirection)2,
				GrowVertical = (GrowDirection)2,
				MouseFilter = (MouseFilterEnum)2
			}, false, (InternalMode)0);
		}
	}

	private void SetupNodeFocusNeighbors()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < _slots.Count; i++)
		{
			if (!_slots[i].IsLocked)
			{
				list.Add(i);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			TextureRect icon = _slots[list[j]].Icon;
			TextureRect val = (TextureRect)((j > 0) ? ((object)_slots[list[j - 1]].Icon) : ((object)icon));
			TextureRect val2 = (TextureRect)((j < list.Count - 1) ? ((object)_slots[list[j + 1]].Icon) : ((object)icon));
			((Control)icon).FocusNeighborTop = ((Node)val).GetPath();
			((Control)icon).FocusNeighborBottom = ((Node)val2).GetPath();
			((Control)icon).FocusNeighborLeft = ((Node)icon).GetPath();
			((Control)icon).FocusNeighborRight = ((Node)icon).GetPath();
		}
		if (list.Count > 0 && _randomizeButton != null && !((BaseButton)_randomizeButton).Disabled)
		{
			TextureRect icon2 = _slots[list[list.Count - 1]].Icon;
			((Control)icon2).FocusNeighborBottom = ((Node)_randomizeButton).GetPath();
			((Control)_randomizeButton).FocusNeighborTop = ((Node)icon2).GetPath();
			((Control)_randomizeButton).FocusNeighborBottom = ((Node)_randomizeButton).GetPath();
			((Control)_randomizeButton).FocusNeighborLeft = ((Node)_randomizeButton).GetPath();
			((Control)_randomizeButton).FocusNeighborRight = ((Node)_randomizeButton).GetPath();
		}
	}

	private void BuildLegendPanel(Control parent)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected O, but got Unknown
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Expected O, but got Unknown
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Expected O, but got Unknown
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Invalid comparison between Unknown and I4
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Expected O, but got Unknown
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Expected O, but got Unknown
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Expected O, but got Unknown
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Expected O, but got Unknown
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		Control val = new Control
		{
			AnchorLeft = 0f,
			AnchorTop = 0.5f,
			AnchorRight = 0f,
			AnchorBottom = 0.5f,
			OffsetLeft = 48f,
			OffsetTop = -220f,
			OffsetRight = 340f,
			OffsetBottom = 200f,
			GrowHorizontal = (GrowDirection)1,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		NinePatchRect val2 = new NinePatchRect
		{
			Modulate = DarkPurple
		};
		Texture2D val3 = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
		if (val3 != null)
		{
			val2.Texture = val3;
			val2.RegionRect = new Rect2(0f, 0f, 48f, 48f);
			val2.PatchMarginLeft = 14;
			val2.PatchMarginTop = 14;
			val2.PatchMarginRight = 13;
			val2.PatchMarginBottom = 14;
		}
		((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		VBoxContainer val4 = new VBoxContainer
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val4).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Control)val4).OffsetLeft = 20f;
		((Control)val4).OffsetTop = 16f;
		((Control)val4).OffsetRight = -20f;
		((Control)val4).OffsetBottom = -16f;
		((Control)val4).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
		((Node)val).AddChild((Node)(object)val4, false, (InternalMode)0);
		LocString val5 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.legend.title");
		((Node)val4).AddChild((Node)(object)CreateStyledText("[center]" + val5.GetFormattedText() + "[/center]", _fontBold, 26, GoldColor), false, (InternalMode)0);
		((Node)val4).AddChild((Node)new Control
		{
			CustomMinimumSize = new Vector2(0f, 4f),
			MouseFilter = (MouseFilterEnum)2
		}, false, (InternalMode)0);
		(MapPointType, string, int)[] legendEntries = LegendEntries;
		for (int i = 0; i < legendEntries.Length; i++)
		{
			(MapPointType, string, int) tuple = legendEntries[i];
			MapPointType item = tuple.Item1;
			string item2 = tuple.Item2;
			int item3 = tuple.Item3;
			HBoxContainer val6 = new HBoxContainer
			{
				MouseFilter = (MouseFilterEnum)2,
				CustomMinimumSize = new Vector2(0f, 40f)
			};
			((Control)val6).AddThemeConstantOverride(StringName.op_Implicit("separation"), 12);
			TextureRect val7 = new TextureRect
			{
				CustomMinimumSize = new Vector2(40f, 40f),
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5,
				MouseFilter = (MouseFilterEnum)2
			};
			if (_iconCache.TryGetValue(item, out Texture2D value) && value != null)
			{
				val7.Texture = value;
			}
			if ((int)item == 0)
			{
				((CanvasItem)val7).SelfModulate = LockedXColor;
			}
			((Node)val6).AddChild((Node)(object)val7, false, (InternalMode)0);
			LocString val8 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.legend." + item2);
			Label val9 = new Label
			{
				Text = val8.GetFormattedText(),
				SizeFlagsHorizontal = (SizeFlags)3,
				VerticalAlignment = (VerticalAlignment)1,
				MouseFilter = (MouseFilterEnum)2
			};
			if (_fontRegular != null)
			{
				((Control)val9).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)_fontRegular);
			}
			((Control)val9).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
			((Control)val9).AddThemeColorOverride(StringName.op_Implicit("font_color"), ((int)item == 0) ? LockedXColor : CreamColor);
			((Node)val6).AddChild((Node)(object)val9, false, (InternalMode)0);
			string formattedText;
			if (item3 > 0)
			{
				LocString val10 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.legend.cost");
				val10.Add("Cost", (decimal)item3);
				formattedText = val10.GetFormattedText();
			}
			else
			{
				formattedText = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.legend.costFree").GetFormattedText();
			}
			Label val11 = new Label
			{
				Text = formattedText,
				HorizontalAlignment = (HorizontalAlignment)2,
				VerticalAlignment = (VerticalAlignment)1,
				CustomMinimumSize = new Vector2(50f, 0f),
				MouseFilter = (MouseFilterEnum)2
			};
			if (_fontBold != null)
			{
				((Control)val11).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)_fontBold);
			}
			((Control)val11).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
			((Control)val11).AddThemeColorOverride(StringName.op_Implicit("font_color"), ((int)item == 0) ? LockedXColor : GoldColor);
			((Node)val6).AddChild((Node)(object)val11, false, (InternalMode)0);
			((Node)val4).AddChild((Node)(object)val6, false, (InternalMode)0);
		}
	}

	private void BuildInstructionsPanel(Control parent)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		Control val = new Control
		{
			AnchorLeft = 1f,
			AnchorTop = 0.5f,
			AnchorRight = 1f,
			AnchorBottom = 0.5f,
			OffsetLeft = -400f,
			OffsetTop = -340f,
			OffsetRight = -48f,
			OffsetBottom = 260f,
			GrowHorizontal = (GrowDirection)0,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		NinePatchRect val2 = new NinePatchRect
		{
			Modulate = DarkPurple
		};
		Texture2D val3 = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
		if (val3 != null)
		{
			val2.Texture = val3;
			val2.RegionRect = new Rect2(0f, 0f, 48f, 48f);
			val2.PatchMarginLeft = 14;
			val2.PatchMarginTop = 14;
			val2.PatchMarginRight = 13;
			val2.PatchMarginBottom = 14;
		}
		((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		VBoxContainer val4 = new VBoxContainer
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val4).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Control)val4).OffsetLeft = 28f;
		((Control)val4).OffsetTop = 20f;
		((Control)val4).OffsetRight = -28f;
		((Control)val4).OffsetBottom = -20f;
		((Control)val4).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		((Node)val).AddChild((Node)(object)val4, false, (InternalMode)0);
		LocString val5 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.instructions.title");
		((Node)val4).AddChild((Node)(object)CreateStyledText("[center]" + val5.GetFormattedText() + "[/center]", _fontBold, 28, GoldColor), false, (InternalMode)0);
		LocString val6 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.instructions.description");
		val6.Add("Budget", (decimal)_minigame.MaxBudget);
		MegaRichTextLabel val7 = CreateStyledText(val6.GetFormattedText(), _fontRegular, 22, CreamColor);
		((Control)val7).CustomMinimumSize = new Vector2(0f, 280f);
		((Control)val7).AddThemeConstantOverride(StringName.op_Implicit("line_separation"), -4);
		((Node)val4).AddChild((Node)(object)val7, false, (InternalMode)0);
	}

	private void BuildProceedButton()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		PackedScene val = GD.Load<PackedScene>("res://scenes/ui/proceed_button.tscn");
		if (val != null)
		{
			_proceedButton = val.Instantiate<NProceedButton>((GenEditState)0);
			((CanvasItem)_proceedButton).Visible = true;
			((Node)this).AddChild((Node)(object)_proceedButton, false, (InternalMode)0);
			Callable val2 = Callable.From((Action)delegate
			{
				_proceedButton.UpdateText(NProceedButton.ProceedLoc);
				((NClickableControl)_proceedButton).Disable();
				_proceedButtonReady = true;
				UpdateProceedButton();
			});
			((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
			((GodotObject)_proceedButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
			{
				_minigame.Confirm();
			}), 0u);
		}
	}

	private void BuildRandomizeButton()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		_randomizeButton = new Button
		{
			Text = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.randomize").GetFormattedText(),
			AnchorTop = 1f,
			AnchorBottom = 1f,
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			OffsetLeft = -90f,
			OffsetTop = -95f,
			OffsetRight = 90f,
			OffsetBottom = -48f,
			GrowVertical = (GrowDirection)0,
			GrowHorizontal = (GrowDirection)2,
			Disabled = _minigame.IsRandomized,
			FocusMode = (FocusModeEnum)2
		};
		if (_fontBold != null)
		{
			((Control)_randomizeButton).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)_fontBold);
		}
		((Control)_randomizeButton).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 24);
		((BaseButton)_randomizeButton).Pressed += delegate
		{
			_minigame.Randomize();
		};
		((GodotObject)_randomizeButton).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)delegate(InputEvent ev)
		{
			if (ev.IsActionPressed(MegaInput.select, false, false) && !((BaseButton)_randomizeButton).Disabled)
			{
				((Control)_randomizeButton).AcceptEvent();
				_minigame.Randomize();
			}
		}), 0u);
		((Node)this).AddChild((Node)(object)_randomizeButton, false, (InternalMode)0);
	}

	private MegaRichTextLabel CreateStyledText(string bbcode, FontVariation? font, int fontSize, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		MegaRichTextLabel val = new MegaRichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			MouseFilter = (MouseFilterEnum)2,
			AutowrapMode = (AutowrapMode)3,
			AutoSizeEnabled = false
		};
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("default_color"), color);
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("normal_font_size"), fontSize);
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("bold_font_size"), fontSize);
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("italics_font_size"), fontSize);
		if (font != null)
		{
			((Control)val).AddThemeFontOverride(StringName.op_Implicit("normal_font"), (Font)(object)font);
		}
		val.Text = bbcode;
		return val;
	}

	private void SetHovered(int index)
	{
		int hoveredIndex = _hoveredIndex;
		_hoveredIndex = index;
		if (hoveredIndex >= 0 && hoveredIndex < _slots.Count)
		{
			ApplySlotColor(hoveredIndex);
		}
		ApplySlotColor(index);
	}

	private void ClearHovered(int index)
	{
		if (_hoveredIndex == index)
		{
			_hoveredIndex = -1;
			ApplySlotColor(index);
		}
	}

	private void OnMinigameFinished()
	{
		NOverlayStack.Instance.Remove((IOverlayScreen)(object)this);
	}

	private void RefreshAll()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Expected O, but got Unknown
		int totalCost = _minigame.TotalCost;
		int maxBudget = _minigame.MaxBudget;
		bool isOverBudget = _minigame.IsOverBudget;
		for (int i = 0; i < _slots.Count; i++)
		{
			NodeSlot nodeSlot = _slots[i];
			if (!nodeSlot.IsLocked)
			{
				MapPointType nodeType = _minigame.GetNodeType(i);
				bool flag = i == _minigame.SelectedIndex;
				bool flag2 = _minigame.IsLocked(i);
				if (_iconCache.TryGetValue(nodeType, out Texture2D value))
				{
					nodeSlot.Icon.Texture = value;
				}
				if (nodeSlot.LeftArrow != null)
				{
					((CanvasItem)nodeSlot.LeftArrow).Visible = flag && !flag2;
				}
				if (nodeSlot.RightArrow != null)
				{
					((CanvasItem)nodeSlot.RightArrow).Visible = flag && !flag2;
				}
				if (flag2)
				{
					((Control)nodeSlot.Icon).MouseFilter = (MouseFilterEnum)2;
				}
				ApplySlotColor(i);
			}
		}
		if (isOverBudget)
		{
			LocString val = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.budgetOver");
			val.Add("Current", (decimal)totalCost);
			val.Add("Max", (decimal)maxBudget);
			_budgetLabel.Text = val.GetFormattedText();
		}
		else
		{
			LocString val2 = new LocString("events", "ACTSFROMTHEPAST-SECRET_PORTAL.minigame.budget");
			val2.Add("Current", (decimal)totalCost);
			val2.Add("Max", (decimal)maxBudget);
			_budgetLabel.Text = val2.GetFormattedText();
		}
		UpdateProceedButton();
	}

	private void ApplySlotColor(int i)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if (i < 0 || i >= _slots.Count)
		{
			return;
		}
		NodeSlot nodeSlot = _slots[i];
		if (!nodeSlot.IsLocked)
		{
			bool flag = i == _minigame.SelectedIndex;
			Color selfModulate = ((_minigame.IsOverBudget && !_minigame.IsRandomized) ? OverBudgetColor : (flag ? SelectedGlow : ((i != _hoveredIndex) ? DimmedColor : Colors.White)));
			((CanvasItem)nodeSlot.Icon).SelfModulate = selfModulate;
			if (flag)
			{
				((Control)nodeSlot.Icon).Scale = Vector2.One * 1.2f;
			}
			else if (i == _hoveredIndex)
			{
				((Control)nodeSlot.Icon).Scale = Vector2.One * 1.1f;
			}
			else
			{
				((Control)nodeSlot.Icon).Scale = Vector2.One;
			}
		}
	}

	private void UpdateProceedButton()
	{
		if (_proceedButton != null && _proceedButtonReady)
		{
			if (_minigame.IsValid)
			{
				((NClickableControl)_proceedButton).Enable();
			}
			else
			{
				((NClickableControl)_proceedButton).Disable();
			}
		}
	}
}
