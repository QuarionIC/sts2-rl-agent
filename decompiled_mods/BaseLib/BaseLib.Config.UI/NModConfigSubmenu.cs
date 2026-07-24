using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BaseLib.Extensions;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NModConfigSubmenu.cs")]
public class NModConfigSubmenu : NSubmenu
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName InitializeModList = StringName.op_Implicit("InitializeModList");

		public static readonly StringName GetActiveModButton = StringName.op_Implicit("GetActiveModButton");

		public static readonly StringName ModButtonFocused = StringName.op_Implicit("ModButtonFocused");

		public static readonly StringName FocusActiveModButton = StringName.op_Implicit("FocusActiveModButton");

		public static readonly StringName SetBackButtonVisible = StringName.op_Implicit("SetBackButtonVisible");

		public static readonly StringName InputTypeChanged = StringName.op_Implicit("InputTypeChanged");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName CreateOptionContainer = StringName.op_Implicit("CreateOptionContainer");

		public static readonly StringName CreateTitleControl = StringName.op_Implicit("CreateTitleControl");

		public static readonly StringName OnGlobalFocusChanged = StringName.op_Implicit("OnGlobalFocusChanged");

		public static readonly StringName RefreshSize = StringName.op_Implicit("RefreshSize");

		public static readonly StringName OnSubmenuShown = StringName.op_Implicit("OnSubmenuShown");

		public static readonly StringName WaitForLayoutAndFadeIn = StringName.op_Implicit("WaitForLayoutAndFadeIn");

		public static readonly StringName OnSubmenuHidden = StringName.op_Implicit("OnSubmenuHidden");

		public static readonly StringName SaveAndClearCurrentMod = StringName.op_Implicit("SaveAndClearCurrentMod");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName CreateControllerNavEcho = StringName.op_Implicit("CreateControllerNavEcho");

		public static readonly StringName SaveCurrentConfig = StringName.op_Implicit("SaveCurrentConfig");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName InitialFocusedControl = StringName.op_Implicit("InitialFocusedControl");

		public static readonly StringName _backButton = StringName.op_Implicit("_backButton");

		public static readonly StringName _leftScrollArea = StringName.op_Implicit("_leftScrollArea");

		public static readonly StringName _modListVbox = StringName.op_Implicit("_modListVbox");

		public static readonly StringName _modListPanel = StringName.op_Implicit("_modListPanel");

		public static readonly StringName _modListTitle = StringName.op_Implicit("_modListTitle");

		public static readonly StringName _rightScrollArea = StringName.op_Implicit("_rightScrollArea");

		public static readonly StringName _optionContainer = StringName.op_Implicit("_optionContainer");

		public static readonly StringName _contentPanel = StringName.op_Implicit("_contentPanel");

		public static readonly StringName _modTitle = StringName.op_Implicit("_modTitle");

		public static readonly StringName _fadeInTween = StringName.op_Implicit("_fadeInTween");

		public static readonly StringName _saveTimer = StringName.op_Implicit("_saveTimer");

		public static readonly StringName _modLoadFailed = StringName.op_Implicit("_modLoadFailed");

		public static readonly StringName _lastFocusOnModList = StringName.op_Implicit("_lastFocusOnModList");

		public static readonly StringName _isUsingController = StringName.op_Implicit("_isUsingController");

		public static readonly StringName _navRepeatTimer = StringName.op_Implicit("_navRepeatTimer");

		public static readonly StringName _heldNavAction = StringName.op_Implicit("_heldNavAction");
	}

	public class SignalName : SignalName
	{
	}

	private NBackButton? _backButton;

	private NNativeScrollableContainer _leftScrollArea;

	private VBoxContainer _modListVbox;

	private Control _modListPanel;

	private MegaRichTextLabel _modListTitle;

	private NNativeScrollableContainer _rightScrollArea;

	private VBoxContainer? _optionContainer;

	private Control _contentPanel;

	private MegaRichTextLabel _modTitle;

	private Tween? _fadeInTween;

	private ModConfig? _currentConfig;

	private double _saveTimer = -1.0;

	private bool _modLoadFailed;

	private bool _lastFocusOnModList = true;

	private const double AutosaveDelay = 5.0;

	private bool _isUsingController;

	private double _navRepeatTimer;

	private StringName? _heldNavAction;

	private const float InitialRepeatDelay = 0.4f;

	private const float RepeatRate = 0.1f;

	private const float ModTitleHeight = 90f;

	private const float TopOffset = 120f;

	private const float ModListPosition = 180f;

	private const float ModListWidth = 360f;

	private const float MaxRightSideWidth = 1200f;

	private const int ModConfigPadding = 16;

	protected override Control? InitialFocusedControl
	{
		get
		{
			if (!_lastFocusOnModList)
			{
				return ((Node?)(object)_optionContainer)?.FindFirstFocusable();
			}
			return (Control?)(object)GetActiveModButton();
		}
	}

	public NModConfigSubmenu()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)this).GrowHorizontal = (GrowDirection)2;
		((Control)this).GrowVertical = (GrowDirection)2;
		_leftScrollArea = new NNativeScrollableContainer(120f);
		_modListPanel = new Control
		{
			Name = StringName.op_Implicit("ModListContent"),
			MouseFilter = (MouseFilterEnum)2
		};
		_modListPanel.SetAnchorsPreset((LayoutPreset)15, false);
		((Node)this).AddChild((Node)(object)_leftScrollArea, false, (InternalMode)0);
		_rightScrollArea = new NNativeScrollableContainer(120f);
		_contentPanel = new Control
		{
			Name = StringName.op_Implicit("ModConfigContent"),
			MouseFilter = (MouseFilterEnum)2
		};
		_contentPanel.SetAnchorsPreset((LayoutPreset)0, false);
		((Node)this).AddChild((Node)(object)_rightScrollArea, false, (InternalMode)0);
		_modListTitle = CreateTitleControl("ModListTitle", "[center]Mods[/center]", 0f);
		((Control)_modListTitle).OffsetLeft = 180f;
		((Control)_modListTitle).OffsetRight = 480f;
		_modTitle = CreateTitleControl("ModTitle", "[center]Unknown mod name[/center]", 0f);
		_modListVbox = new VBoxContainer();
	}

	public override void _Ready()
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		((Node)this).AddChild((Node)(object)_modTitle, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)_modListTitle, false, (InternalMode)0);
		((Node)_modListPanel).AddChild((Node)(object)_modListVbox, false, (InternalMode)0);
		_modListPanel.SetAnchorsPreset((LayoutPreset)0, false);
		InitializeModList();
		((Control)_modListVbox).MinimumSizeChanged += delegate
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			_modListPanel.CustomMinimumSize = new Vector2(_leftScrollArea.AvailableContentWidth, ((Control)_modListVbox).GetMinimumSize().Y);
		};
		_leftScrollArea.AttachContent(_modListPanel);
		((NScrollableContainer)_leftScrollArea).DisableScrollingIfContentFits();
		_rightScrollArea.AttachContent(_contentPanel);
		((NScrollableContainer)_rightScrollArea).DisableScrollingIfContentFits();
		_backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button")).Instantiate<NBackButton>((GenEditState)0);
		((Node)_backButton).Name = StringName.op_Implicit("BackButton");
		((Node)this).AddChild((Node)(object)_backButton, false, (InternalMode)0);
		NControllerManager instance = NControllerManager.Instance;
		_isUsingController = instance != null && instance.IsUsingController;
		((NSubmenu)this).ConnectSignals();
		((GodotObject)((Node)this).GetViewport()).Connect(SignalName.SizeChanged, Callable.From((Action)RefreshSize), 0u);
		((GodotObject)((Node)this).GetViewport()).Connect(SignalName.GuiFocusChanged, Callable.From<Control>((Action<Control>)OnGlobalFocusChanged), 0u);
		NControllerManager instance2 = NControllerManager.Instance;
		if (instance2 != null)
		{
			((GodotObject)instance2).Connect(SignalName.MouseDetected, Callable.From((Action)InputTypeChanged), 0u);
		}
		NControllerManager instance3 = NControllerManager.Instance;
		if (instance3 != null)
		{
			((GodotObject)instance3).Connect(SignalName.ControllerDetected, Callable.From((Action)InputTypeChanged), 0u);
		}
	}

	private void InitializeModList()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Expected O, but got Unknown
		NodePath val = new NodePath(".");
		foreach (ModConfig modConfig in from mod in ModConfigRegistry.GetAll()
			where mod.VisibleInModList()
			select mod)
		{
			NModListButton nModListButton = new NModListButton(GetModTitle(modConfig));
			((Node)_modListVbox).AddChild((Node)(object)nModListButton, false, (InternalMode)0);
			((GodotObject)nModListButton).Connect(SignalName.Released, Callable.From<NModListButton>((Action<NModListButton>)delegate(NModListButton button)
			{
				ModButtonClicked(button, modConfig);
			}), 0u);
			((GodotObject)nModListButton).Connect(SignalName.Focused, Callable.From<NModListButton>((Action<NModListButton>)ModButtonFocused), 0u);
			((Control)nModListButton).FocusNeighborLeft = val;
			((Control)nModListButton).FocusNeighborRight = val;
		}
		Array<Node> children = ((Node)_modListVbox).GetChildren(false);
		NModListButton nModListButton2 = ((IEnumerable<Node>)children).First() as NModListButton;
		NModListButton nModListButton3 = ((IEnumerable<Node>)children).Last() as NModListButton;
		if (nModListButton2 != null)
		{
			((Control)nModListButton2).FocusNeighborTop = ((Node)nModListButton2).GetPathTo((Node)(object)nModListButton3, false);
		}
		if (nModListButton3 != null)
		{
			((Control)nModListButton3).FocusNeighborBottom = ((Node)nModListButton3).GetPathTo((Node)(object)nModListButton2, false);
		}
		Control val2 = new Control
		{
			CustomMinimumSize = new Vector2(0f, 20f)
		};
		((Node)_modListVbox).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)_modListVbox).MoveChild((Node)(object)val2, 0);
		((Node)_modListVbox).AddChild((Node)new Control
		{
			CustomMinimumSize = new Vector2(0f, 24f)
		}, false, (InternalMode)0);
	}

	private NModListButton? GetActiveModButton()
	{
		if (_currentConfig == null)
		{
			return null;
		}
		foreach (Node child in ((Node)_modListPanel).GetChild(0, false).GetChildren(false))
		{
			if (child is NModListButton nModListButton && nModListButton.ModName == GetModTitle(_currentConfig))
			{
				return nModListButton;
			}
		}
		return null;
	}

	private void ModButtonClicked(NModListButton button, ModConfig modConfig)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (modConfig != _currentConfig)
		{
			LoadModConfig(modConfig);
		}
		if (!_isUsingController || _modLoadFailed)
		{
			return;
		}
		button.SetHotkeyIconVisible(enabled: true);
		Callable val = Callable.From((Action)delegate
		{
			VBoxContainer? optionContainer = _optionContainer;
			if (optionContainer != null)
			{
				Control? obj = ((Node?)(object)optionContainer).FindFirstFocusable();
				if (obj != null)
				{
					NodeUtil.TryGrabFocus(obj);
				}
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private void ModButtonFocused(NModListButton button)
	{
		SetBackButtonVisible(visible: true);
	}

	private void FocusActiveModButton()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Callable val = Callable.From((Action)delegate
		{
			NModListButton? activeModButton = GetActiveModButton();
			if (activeModButton != null)
			{
				NodeUtil.TryGrabFocus((Control)(object)activeModButton);
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private void SetBackButtonVisible(bool visible)
	{
		if (_backButton != null)
		{
			if (!visible)
			{
				((NClickableControl)_backButton).Disable();
				return;
			}
			((NClickableControl)_backButton)._isEnabled = false;
			((NClickableControl)_backButton).Enable();
		}
	}

	private void SetHighlightedModButton(ModConfig config)
	{
		foreach (Node child in ((Node)_modListPanel).GetChild(0, false).GetChildren(false))
		{
			if (child is NModListButton nModListButton)
			{
				nModListButton.SetActiveState(nModListButton.ModName == GetModTitle(config));
			}
		}
	}

	private void InputTypeChanged()
	{
		NControllerManager instance = NControllerManager.Instance;
		_isUsingController = instance != null && instance.IsUsingController;
		SetBackButtonVisible(visible: true);
		FocusActiveModButton();
	}

	public override void _Input(InputEvent @event)
	{
		((Node)this)._Input(@event);
		NBackButton? backButton = _backButton;
		if ((backButton != null && ((NClickableControl)backButton).IsEnabled) || (!@event.IsActionReleased(MegaInput.cancel, false) && !@event.IsActionReleased(MegaInput.pauseAndBack, false) && !@event.IsActionReleased(MegaInput.back, false)))
		{
			return;
		}
		Control val = ((Node)this).GetViewport().GuiGetFocusOwner();
		if (val != null)
		{
			VBoxContainer? optionContainer = _optionContainer;
			if (optionContainer != null && ((Node)optionContainer).IsAncestorOf((Node)(object)val))
			{
				FocusActiveModButton();
				((Control)this).AcceptEvent();
			}
		}
	}

	private void LoadModConfig(ModConfig config)
	{
		if (config.ModId != null)
		{
			BaseLibConfig.LastModConfigModId = config.ModId;
		}
		if (_optionContainer != null || _currentConfig != null)
		{
			SaveAndClearCurrentMod();
		}
		_currentConfig = config;
		config.ConfigChanged += OnConfigChanged;
		SetHighlightedModButton(config);
		_optionContainer = CreateOptionContainer();
		((Node)_contentPanel).AddChild((Node)(object)_optionContainer, false, (InternalMode)0);
		try
		{
			config.SetupConfigUI((Control)(object)_optionContainer);
			_modLoadFailed = false;
		}
		catch (Exception value)
		{
			_modLoadFailed = true;
			SaveAndClearCurrentMod();
			_currentConfig = config;
			_optionContainer = CreateOptionContainer();
			((Node)_contentPanel).AddChild((Node)(object)_optionContainer, false, (InternalMode)0);
			string modTitle = GetModTitle(config);
			MegaRichTextLabel val = ModConfig.CreateRawLabelControl("[center]BaseLib failed setting up the mod config for " + modTitle + ".\nThis is either because the mod set something up incorrectly, or a compatibility issue.\nTry updating BaseLib and " + modTitle + ", if newer versions exist.[/center]", 32);
			((RichTextLabel)val).FitContent = true;
			((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
			((Node)_optionContainer).AddChild((Node)(object)val, false, (InternalMode)0);
			BaseLibMain.Logger.Error($"SetupConfigUI failed for mod {modTitle}: {value}", 1);
		}
		try
		{
			string textAutoSize = "[center]" + GetModTitle(config) + "[/center]";
			_modTitle.SetTextAutoSize(textAutoSize);
			RefreshSize();
			((NScrollableContainer)_rightScrollArea).InstantlyScrollToTop();
			ModConfig.ShowAndClearPendingErrors();
		}
		catch (Exception ex)
		{
			ModConfig.ModConfigLogger.Error("An error occurred while loading the mod config screen.\nPlease report a bug at:\nhttps://github.com/Alchyr/BaseLib-StS2");
			BaseLibMain.Logger.Error(ex.ToString(), 1);
			base._stack.Pop();
		}
	}

	private VBoxContainer CreateOptionContainer()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		VBoxContainer val = new VBoxContainer
		{
			Name = StringName.op_Implicit("VBoxContainer"),
			CustomMinimumSize = new Vector2(0f, 0f),
			AnchorRight = 1f,
			GrowHorizontal = (GrowDirection)1,
			FocusMode = (FocusModeEnum)0,
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)val).AddChild((Node)new Control
		{
			CustomMinimumSize = new Vector2(0f, 16f)
		}, false, (InternalMode)0);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		((Control)val).MinimumSizeChanged += RefreshSize;
		return val;
	}

	private static MegaRichTextLabel CreateTitleControl(string name, string defaultText, float minimumWidth)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		MegaRichTextLabel obj = ModConfig.CreateRawLabelControl(defaultText, 36);
		((Node)obj).Name = StringName.op_Implicit(name);
		obj.AutoSizeEnabled = true;
		obj.MaxFontSize = 64;
		((Control)obj).CustomMinimumSize = new Vector2(minimumWidth, 90f);
		((Control)obj).SetAnchorsPreset((LayoutPreset)0, false);
		((Control)obj).OffsetBottom = 110f;
		((Control)obj).OffsetTop = ((Control)obj).OffsetBottom - 90f;
		return obj;
	}

	private static string GetModTitle(ModConfig config)
	{
		string modPrefix = config.ModPrefix;
		string text = modPrefix.Substring(0, modPrefix.Length - 1) + ".mod_title";
		LocString ifExists = LocString.GetIfExists("settings_ui", text);
		if (ifExists != null)
		{
			return ifExists.GetFormattedText();
		}
		ModConfig.ModConfigLogger.Warn("No " + text + " found in localization table, using fallback title");
		string text2 = config.GetType().GetRootNamespace();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = LocString.GetIfExists("settings_ui", "BASELIB-UNKNOWN_MOD_NAME").GetFormattedText();
		}
		return text2;
	}

	private void OnGlobalFocusChanged(Control newFocus)
	{
		if (!((CanvasItem)this).IsVisibleInTree())
		{
			return;
		}
		bool flag = ((Node)_leftScrollArea).IsAncestorOf((Node)(object)newFocus);
		bool num = flag && !_lastFocusOnModList;
		bool flag2 = !flag && _lastFocusOnModList;
		_lastFocusOnModList = flag;
		if (num)
		{
			SetBackButtonVisible(visible: true);
			foreach (Node child in ((Node)_modListVbox).GetChildren(false))
			{
				if (child is NModListButton nModListButton)
				{
					nModListButton.SetHotkeyIconVisible(enabled: false);
				}
			}
			if ((object)newFocus != GetActiveModButton())
			{
				FocusActiveModButton();
			}
		}
		else if (flag2 && _isUsingController)
		{
			SetBackButtonVisible(visible: false);
		}
	}

	private void RefreshSize()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		if (_optionContainer != null)
		{
			Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
			Vector2 size = ((Rect2)(ref viewportRect)).Size;
			float num = default(float);
			float num2 = default(float);
			((Vector2)(ref size)).Deconstruct(ref num, ref num2);
			float num3 = num;
			float num4 = num2;
			((Control)_leftScrollArea).Position = new Vector2(180f, 0f);
			((Control)_leftScrollArea).Size = new Vector2(360f, num4);
			float availableContentWidth = _leftScrollArea.AvailableContentWidth;
			_modListPanel.CustomMinimumSize = new Vector2(availableContentWidth, _modListPanel.CustomMinimumSize.Y);
			((Control)_modListVbox).CustomMinimumSize = new Vector2(availableContentWidth, ((Control)_modListVbox).CustomMinimumSize.Y);
			((Control)_modListVbox).Size = new Vector2(availableContentWidth, ((Control)_modListVbox).Size.Y);
			float num5 = num3 - 540f;
			float num6 = Mathf.Min(num5 - 64f - 60f - 32f, 1108f);
			float num7 = num5 - num6 - 60f - 32f - 64f;
			float num8 = 0f;
			float num9 = 0f;
			if (num7 > 0f)
			{
				num8 = Mathf.Min(num7, 64f);
				num7 -= num8;
				num9 = num7 / 2f;
			}
			float num10 = 572f + num9;
			float num11 = num6 + 32f + num8 + 60f;
			float num12 = num10 - 16f;
			((Control)_rightScrollArea).Position = new Vector2(num12, 0f);
			((Control)_rightScrollArea).Size = new Vector2(num11, num4);
			((Control)_optionContainer).Position = new Vector2(16f, 0f);
			((Control)_optionContainer).CustomMinimumSize = new Vector2(num6, 0f);
			((Control)_optionContainer).Size = new Vector2(num6, 0f);
			float y = ((Control)_optionContainer).GetMinimumSize().Y;
			float num13 = y + 30f;
			Vector2 size2 = ((Node)_contentPanel).GetParent<Control>().Size;
			if (num13 >= size2.Y)
			{
				num13 += size2.Y * 0.3f;
			}
			float availableContentWidth2 = _rightScrollArea.AvailableContentWidth;
			_contentPanel.CustomMinimumSize = new Vector2(availableContentWidth2, num13);
			_contentPanel.Size = new Vector2(availableContentWidth2, num13);
			((Control)_optionContainer).Size = new Vector2(num6, y);
			((Control)_modTitle).OffsetLeft = num10;
			((Control)_modTitle).OffsetRight = num10 + num6;
			((Control)_modTitle).CustomMinimumSize = new Vector2(num6, 90f);
		}
	}

	protected override void OnSubmenuShown()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		((NSubmenu)this).OnSubmenuShown();
		((CanvasItem)_contentPanel).Modulate = new Color(1f, 1f, 1f, 0f);
		_saveTimer = -1.0;
		BaseLibConfig baseLibConfig = ModConfigRegistry.Get<BaseLibConfig>();
		string lastModConfigModId = BaseLibConfig.LastModConfigModId;
		ModConfig modConfig = ((!string.IsNullOrWhiteSpace(lastModConfigModId)) ? ModConfigRegistry.Get(lastModConfigModId) : baseLibConfig);
		LoadModConfig(modConfig ?? baseLibConfig);
		Callable val = Callable.From((Action)InputTypeChanged);
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		WaitForLayoutAndFadeIn();
	}

	private async void WaitForLayoutAndFadeIn()
	{
		_ = 1;
		try
		{
			await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
			await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
			_leftScrollArea.ScrollToFocusedControl(skipAnimation: true);
		}
		catch (Exception ex)
		{
			BaseLibMain.Logger.Error(ex.ToString(), 1);
		}
		finally
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)this) && ((Node)this).IsInsideTree())
			{
				Tween? fadeInTween = _fadeInTween;
				if (fadeInTween != null)
				{
					fadeInTween.Kill();
				}
				_fadeInTween = ((Node)this).CreateTween().SetParallel(true);
				_fadeInTween.TweenProperty((GodotObject)(object)_contentPanel, NodePath.op_Implicit("modulate"), Variant.op_Implicit(Colors.White), 0.5).From(Variant.op_Implicit(new Color(0f, 0f, 0f, 0f))).SetEase((EaseType)1)
					.SetTrans((TransitionType)7);
			}
		}
	}

	protected override void OnSubmenuHidden()
	{
		SaveAndClearCurrentMod();
		((NSubmenu)this).OnSubmenuHidden();
	}

	private void SaveAndClearCurrentMod()
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		if (_currentConfig != null)
		{
			_currentConfig.ConfigChanged -= OnConfigChanged;
		}
		SaveCurrentConfig();
		if (_optionContainer != null)
		{
			((Control)_optionContainer).MinimumSizeChanged -= RefreshSize;
			GodotTreeExtensions.QueueFreeSafely((Node)(object)_optionContainer);
			_optionContainer = null;
		}
		if (_currentConfig is SimpleModConfig simpleModConfig)
		{
			simpleModConfig.ClearUIEventHandlers();
		}
		_currentConfig = null;
		if (ModConfig.ModConfigLogger.PendingUserMessages.Count > 0)
		{
			Callable val = Callable.From((Action)ModConfig.ShowAndClearPendingErrors);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private void OnConfigChanged(object? sender, EventArgs e)
	{
		_saveTimer = 5.0;
	}

	public override void _Process(double delta)
	{
		((Node)this)._Process(delta);
		if (_isUsingController)
		{
			CreateControllerNavEcho(delta);
		}
		if (!(_saveTimer <= 0.0))
		{
			_saveTimer -= delta;
			if (_saveTimer <= 0.0)
			{
				SaveCurrentConfig();
			}
		}
	}

	private void CreateControllerNavEcho(double delta)
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		StringName val = (Input.IsActionPressed(MegaInput.down, false) ? MegaInput.down : (Input.IsActionPressed(MegaInput.up, false) ? MegaInput.up : null));
		if (val != _heldNavAction)
		{
			_heldNavAction = val;
			_navRepeatTimer = 0.4000000059604645;
		}
		else if (!(val == (StringName)null))
		{
			_navRepeatTimer -= delta;
			if (!(_navRepeatTimer > 0.0))
			{
				_navRepeatTimer = 0.10000000149011612;
				Input.ParseInputEvent((InputEvent)new InputEventAction
				{
					Action = val,
					Pressed = true
				});
			}
		}
	}

	private void SaveCurrentConfig()
	{
		_saveTimer = -1.0;
		if (_modLoadFailed)
		{
			BaseLibMain.Logger.Warn("Ignoring SaveCurrentConfig for " + _currentConfig?.ModId + ": UI setup failed", 1);
		}
		else
		{
			_currentConfig?.Save();
		}
	}

	public override void _ExitTree()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)((Node)this).GetViewport()).Disconnect(SignalName.SizeChanged, Callable.From((Action)RefreshSize));
		((GodotObject)((Node)this).GetViewport()).Disconnect(SignalName.GuiFocusChanged, Callable.From<Control>((Action<Control>)OnGlobalFocusChanged));
		NControllerManager instance = NControllerManager.Instance;
		if (instance != null)
		{
			((GodotObject)instance).Disconnect(SignalName.MouseDetected, Callable.From((Action)InputTypeChanged));
		}
		NControllerManager instance2 = NControllerManager.Instance;
		if (instance2 != null)
		{
			((GodotObject)instance2).Disconnect(SignalName.ControllerDetected, Callable.From((Action)InputTypeChanged));
		}
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Expected O, but got Unknown
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Expected O, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Expected O, but got Unknown
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Expected O, but got Unknown
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(20)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.InitializeModList, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetActiveModButton, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ModButtonFocused, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("button"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.FocusActiveModButton, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetBackButtonVisible, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("visible"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.InputTypeChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateOptionContainer, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("VBoxContainer"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateTitleControl, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("RichTextLabel"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("defaultText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("minimumWidth"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnGlobalFocusChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("newFocus"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshSize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnSubmenuShown, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.WaitForLayoutAndFadeIn, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnSubmenuHidden, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SaveAndClearCurrentMod, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateControllerNavEcho, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SaveCurrentConfig, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InitializeModList && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitializeModList();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetActiveModButton && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			NModListButton activeModButton = GetActiveModButton();
			ret = VariantUtils.CreateFrom<NModListButton>(ref activeModButton);
			return true;
		}
		if ((ref method) == MethodName.ModButtonFocused && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ModButtonFocused(VariantUtils.ConvertTo<NModListButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FocusActiveModButton && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			FocusActiveModButton();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetBackButtonVisible && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetBackButtonVisible(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InputTypeChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InputTypeChanged();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateOptionContainer && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			VBoxContainer val = CreateOptionContainer();
			ret = VariantUtils.CreateFrom<VBoxContainer>(ref val);
			return true;
		}
		if ((ref method) == MethodName.CreateTitleControl && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			MegaRichTextLabel val2 = CreateTitleControl(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<MegaRichTextLabel>(ref val2);
			return true;
		}
		if ((ref method) == MethodName.OnGlobalFocusChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnGlobalFocusChanged(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshSize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshSize();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnSubmenuShown && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NSubmenu)this).OnSubmenuShown();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.WaitForLayoutAndFadeIn && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			WaitForLayoutAndFadeIn();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnSubmenuHidden && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NSubmenu)this).OnSubmenuHidden();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SaveAndClearCurrentMod && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SaveAndClearCurrentMod();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateControllerNavEcho && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			CreateControllerNavEcho(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SaveCurrentConfig && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SaveCurrentConfig();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((NSubmenu)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.CreateTitleControl && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			MegaRichTextLabel val = CreateTitleControl(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<MegaRichTextLabel>(ref val);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.InitializeModList)
		{
			return true;
		}
		if ((ref method) == MethodName.GetActiveModButton)
		{
			return true;
		}
		if ((ref method) == MethodName.ModButtonFocused)
		{
			return true;
		}
		if ((ref method) == MethodName.FocusActiveModButton)
		{
			return true;
		}
		if ((ref method) == MethodName.SetBackButtonVisible)
		{
			return true;
		}
		if ((ref method) == MethodName.InputTypeChanged)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateOptionContainer)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateTitleControl)
		{
			return true;
		}
		if ((ref method) == MethodName.OnGlobalFocusChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshSize)
		{
			return true;
		}
		if ((ref method) == MethodName.OnSubmenuShown)
		{
			return true;
		}
		if ((ref method) == MethodName.WaitForLayoutAndFadeIn)
		{
			return true;
		}
		if ((ref method) == MethodName.OnSubmenuHidden)
		{
			return true;
		}
		if ((ref method) == MethodName.SaveAndClearCurrentMod)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateControllerNavEcho)
		{
			return true;
		}
		if ((ref method) == MethodName.SaveCurrentConfig)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((NSubmenu)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._backButton)
		{
			_backButton = VariantUtils.ConvertTo<NBackButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._leftScrollArea)
		{
			_leftScrollArea = VariantUtils.ConvertTo<NNativeScrollableContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modListVbox)
		{
			_modListVbox = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modListPanel)
		{
			_modListPanel = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modListTitle)
		{
			_modListTitle = VariantUtils.ConvertTo<MegaRichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rightScrollArea)
		{
			_rightScrollArea = VariantUtils.ConvertTo<NNativeScrollableContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._optionContainer)
		{
			_optionContainer = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._contentPanel)
		{
			_contentPanel = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modTitle)
		{
			_modTitle = VariantUtils.ConvertTo<MegaRichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fadeInTween)
		{
			_fadeInTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._saveTimer)
		{
			_saveTimer = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modLoadFailed)
		{
			_modLoadFailed = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lastFocusOnModList)
		{
			_lastFocusOnModList = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isUsingController)
		{
			_isUsingController = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._navRepeatTimer)
		{
			_navRepeatTimer = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._heldNavAction)
		{
			_heldNavAction = VariantUtils.ConvertTo<StringName>(ref value);
			return true;
		}
		return ((NSubmenu)this).SetGodotClassPropertyValue(ref name, ref value);
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
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.InitialFocusedControl)
		{
			Control initialFocusedControl = ((NSubmenu)this).InitialFocusedControl;
			value = VariantUtils.CreateFrom<Control>(ref initialFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName._backButton)
		{
			value = VariantUtils.CreateFrom<NBackButton>(ref _backButton);
			return true;
		}
		if ((ref name) == PropertyName._leftScrollArea)
		{
			value = VariantUtils.CreateFrom<NNativeScrollableContainer>(ref _leftScrollArea);
			return true;
		}
		if ((ref name) == PropertyName._modListVbox)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _modListVbox);
			return true;
		}
		if ((ref name) == PropertyName._modListPanel)
		{
			value = VariantUtils.CreateFrom<Control>(ref _modListPanel);
			return true;
		}
		if ((ref name) == PropertyName._modListTitle)
		{
			value = VariantUtils.CreateFrom<MegaRichTextLabel>(ref _modListTitle);
			return true;
		}
		if ((ref name) == PropertyName._rightScrollArea)
		{
			value = VariantUtils.CreateFrom<NNativeScrollableContainer>(ref _rightScrollArea);
			return true;
		}
		if ((ref name) == PropertyName._optionContainer)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _optionContainer);
			return true;
		}
		if ((ref name) == PropertyName._contentPanel)
		{
			value = VariantUtils.CreateFrom<Control>(ref _contentPanel);
			return true;
		}
		if ((ref name) == PropertyName._modTitle)
		{
			value = VariantUtils.CreateFrom<MegaRichTextLabel>(ref _modTitle);
			return true;
		}
		if ((ref name) == PropertyName._fadeInTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _fadeInTween);
			return true;
		}
		if ((ref name) == PropertyName._saveTimer)
		{
			value = VariantUtils.CreateFrom<double>(ref _saveTimer);
			return true;
		}
		if ((ref name) == PropertyName._modLoadFailed)
		{
			value = VariantUtils.CreateFrom<bool>(ref _modLoadFailed);
			return true;
		}
		if ((ref name) == PropertyName._lastFocusOnModList)
		{
			value = VariantUtils.CreateFrom<bool>(ref _lastFocusOnModList);
			return true;
		}
		if ((ref name) == PropertyName._isUsingController)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isUsingController);
			return true;
		}
		if ((ref name) == PropertyName._navRepeatTimer)
		{
			value = VariantUtils.CreateFrom<double>(ref _navRepeatTimer);
			return true;
		}
		if ((ref name) == PropertyName._heldNavAction)
		{
			value = VariantUtils.CreateFrom<StringName>(ref _heldNavAction);
			return true;
		}
		return ((NSubmenu)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._backButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._leftScrollArea, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._modListVbox, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._modListPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._modListTitle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._rightScrollArea, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._optionContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._contentPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._modTitle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fadeInTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._saveTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._modLoadFailed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._lastFocusOnModList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isUsingController, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._navRepeatTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)21, PropertyName._heldNavAction, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.InitialFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		((NSubmenu)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._backButton, Variant.From<NBackButton>(ref _backButton));
		info.AddProperty(PropertyName._leftScrollArea, Variant.From<NNativeScrollableContainer>(ref _leftScrollArea));
		info.AddProperty(PropertyName._modListVbox, Variant.From<VBoxContainer>(ref _modListVbox));
		info.AddProperty(PropertyName._modListPanel, Variant.From<Control>(ref _modListPanel));
		info.AddProperty(PropertyName._modListTitle, Variant.From<MegaRichTextLabel>(ref _modListTitle));
		info.AddProperty(PropertyName._rightScrollArea, Variant.From<NNativeScrollableContainer>(ref _rightScrollArea));
		info.AddProperty(PropertyName._optionContainer, Variant.From<VBoxContainer>(ref _optionContainer));
		info.AddProperty(PropertyName._contentPanel, Variant.From<Control>(ref _contentPanel));
		info.AddProperty(PropertyName._modTitle, Variant.From<MegaRichTextLabel>(ref _modTitle));
		info.AddProperty(PropertyName._fadeInTween, Variant.From<Tween>(ref _fadeInTween));
		info.AddProperty(PropertyName._saveTimer, Variant.From<double>(ref _saveTimer));
		info.AddProperty(PropertyName._modLoadFailed, Variant.From<bool>(ref _modLoadFailed));
		info.AddProperty(PropertyName._lastFocusOnModList, Variant.From<bool>(ref _lastFocusOnModList));
		info.AddProperty(PropertyName._isUsingController, Variant.From<bool>(ref _isUsingController));
		info.AddProperty(PropertyName._navRepeatTimer, Variant.From<double>(ref _navRepeatTimer));
		info.AddProperty(PropertyName._heldNavAction, Variant.From<StringName>(ref _heldNavAction));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NSubmenu)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._backButton, ref val))
		{
			_backButton = ((Variant)(ref val)).As<NBackButton>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._leftScrollArea, ref val2))
		{
			_leftScrollArea = ((Variant)(ref val2)).As<NNativeScrollableContainer>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._modListVbox, ref val3))
		{
			_modListVbox = ((Variant)(ref val3)).As<VBoxContainer>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._modListPanel, ref val4))
		{
			_modListPanel = ((Variant)(ref val4)).As<Control>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._modListTitle, ref val5))
		{
			_modListTitle = ((Variant)(ref val5)).As<MegaRichTextLabel>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._rightScrollArea, ref val6))
		{
			_rightScrollArea = ((Variant)(ref val6)).As<NNativeScrollableContainer>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._optionContainer, ref val7))
		{
			_optionContainer = ((Variant)(ref val7)).As<VBoxContainer>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._contentPanel, ref val8))
		{
			_contentPanel = ((Variant)(ref val8)).As<Control>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._modTitle, ref val9))
		{
			_modTitle = ((Variant)(ref val9)).As<MegaRichTextLabel>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._fadeInTween, ref val10))
		{
			_fadeInTween = ((Variant)(ref val10)).As<Tween>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._saveTimer, ref val11))
		{
			_saveTimer = ((Variant)(ref val11)).As<double>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._modLoadFailed, ref val12))
		{
			_modLoadFailed = ((Variant)(ref val12)).As<bool>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._lastFocusOnModList, ref val13))
		{
			_lastFocusOnModList = ((Variant)(ref val13)).As<bool>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._isUsingController, ref val14))
		{
			_isUsingController = ((Variant)(ref val14)).As<bool>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._navRepeatTimer, ref val15))
		{
			_navRepeatTimer = ((Variant)(ref val15)).As<double>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName._heldNavAction, ref val16))
		{
			_heldNavAction = ((Variant)(ref val16)).As<StringName>();
		}
	}
}
