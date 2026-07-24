using System;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ActsFromThePast.Minigames;

public class NWheelSpinScreen : Control, IOverlayScreen, IScreenContext
{
	private const float WheelDisplaySize = 1024f;

	private const float ArrowDisplaySize = 512f;

	private const float ButtonDisplaySize = 512f;

	private const float ArrowOffsetX = 480f;

	private const float WheelAngleOffset = 0f;

	private const float ButtonCenterX = -460f;

	private const float ButtonFinalY = 330f;

	private const float ButtonStartY = 900f;

	private const float BounceInDuration = 1.5f;

	private const float ButtonSlideInDuration = 0.6f;

	private const float ButtonSlideOutDuration = 0.4f;

	private const float SpinDuration = 2f;

	private const float SpinVelocity = 1500f;

	private const float DecelerateDuration = 3f;

	private const float PauseDuration = 1f;

	private const float BounceOutDuration = 0.8f;

	private const float WheelStartOffset = -600f;

	private const float WheelBaseY = 50f;

	private const string EventRegionName = "event";

	private static NWheelSpinScreen? _instance;

	private WheelSpinMinigame _minigame = null;

	private TextureRect _wheelRect = null;

	private TextureRect _arrowRect = null;

	private TextureRect _buttonRect = null;

	private TextureRect _buttonGlowRect = null;

	private NProceedButton? _controllerButton;

	private TextureRect? _controllerIcon;

	private Tween? _mainTween;

	private Tween? _buttonTween;

	private Tween? _glowTween;

	private Tween? _spawnTween;

	private Control _particleContainer = null;

	private float _wheelSlideOffset = -600f;

	private float _buttonY = 900f;

	private bool _spinning;

	public NetScreenType ScreenType => (NetScreenType)0;

	public bool UseSharedBackstop => false;

	public Control DefaultFocusedControl => (Control)(object)this;

	private static string GetEventAtlasPath(int actIndex)
	{
		if (1 == 0)
		{
		}
		string result = actIndex switch
		{
			0 => "res://ActsFromThePast/backgrounds/exordium/scene.atlas", 
			1 => "res://ActsFromThePast/backgrounds/city/scene.atlas", 
			_ => "res://ActsFromThePast/backgrounds/beyond/scene.atlas", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static NWheelSpinScreen ShowScreen(WheelSpinMinigame minigame)
	{
		if (_instance != null && GodotObject.IsInstanceValid((GodotObject)(object)_instance))
		{
			((Node)_instance).QueueFree();
		}
		NWheelSpinScreen nWheelSpinScreen = new NWheelSpinScreen();
		nWheelSpinScreen._minigame = minigame;
		nWheelSpinScreen.BuildUI();
		nWheelSpinScreen.BindEvents();
		_instance = nWheelSpinScreen;
		NOverlayStack.Instance.Push((IOverlayScreen)(object)nWheelSpinScreen);
		nWheelSpinScreen.StartBounceIn();
		return nWheelSpinScreen;
	}

	private void BindEvents()
	{
		_minigame.Finished += OnMinigameFinished;
	}

	private void UnbindEvents()
	{
		_minigame.Finished -= OnMinigameFinished;
	}

	public override void _ExitTree()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (NControllerManager.Instance != null)
		{
			((GodotObject)NControllerManager.Instance).Disconnect(SignalName.MouseDetected, Callable.From((Action)UpdateControllerIcon));
			((GodotObject)NControllerManager.Instance).Disconnect(SignalName.ControllerDetected, Callable.From((Action)UpdateControllerIcon));
		}
		UnbindEvents();
		KillAllTweens();
		_minigame.ForceEnd();
		_instance = null;
	}

	private void KillAllTweens()
	{
		Tween? mainTween = _mainTween;
		if (mainTween != null)
		{
			mainTween.Kill();
		}
		Tween? buttonTween = _buttonTween;
		if (buttonTween != null)
		{
			buttonTween.Kill();
		}
		Tween? glowTween = _glowTween;
		if (glowTween != null)
		{
			glowTween.Kill();
		}
		Tween? spawnTween = _spawnTween;
		if (spawnTween != null)
		{
			spawnTween.Kill();
		}
	}

	public void AfterOverlayOpened()
	{
	}

	public void AfterOverlayClosed()
	{
		KillAllTweens();
		GodotTreeExtensions.QueueFreeSafely((Node)(object)this);
	}

	public void AfterOverlayShown()
	{
	}

	public void AfterOverlayHidden()
	{
	}

	private void BuildUI()
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Expected O, but got Unknown
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Expected O, but got Unknown
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Expected O, but got Unknown
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Expected O, but got Unknown
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0529: Unknown result type (might be due to invalid IL or missing references)
		//IL_0532: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_059e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Expected O, but got Unknown
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_0646: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0616: Unknown result type (might be due to invalid IL or missing references)
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion(GetEventAtlasPath(_minigame.ActIndex), "event");
		if (region.HasValue)
		{
			AtlasTexture val = new AtlasTexture();
			val.Atlas = region.Value.Texture;
			val.Region = region.Value.Region;
			TextureRect val2 = new TextureRect
			{
				Texture = (Texture2D)(object)val,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)6,
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
			((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
		}
		_particleContainer = new Control
		{
			MouseFilter = (MouseFilterEnum)2
		};
		_particleContainer.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)_particleContainer, false, (InternalMode)0);
		Texture2D texture = GD.Load<Texture2D>("res://images/event_extras/wheel.png");
		float num = 512f;
		_wheelRect = new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(1024f, 1024f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			PivotOffset = new Vector2(num, num),
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = 0f - num,
			OffsetTop = 0f - num,
			OffsetRight = num,
			OffsetBottom = num,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)this).AddChild((Node)(object)_wheelRect, false, (InternalMode)0);
		Texture2D texture2 = GD.Load<Texture2D>("res://images/event_extras/wheelArrow.png");
		float num2 = 256f;
		_arrowRect = new TextureRect
		{
			Texture = texture2,
			CustomMinimumSize = new Vector2(512f, 512f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = 480f - num2,
			OffsetTop = 0f - num2,
			OffsetRight = 480f + num2,
			OffsetBottom = num2,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)this).AddChild((Node)(object)_arrowRect, false, (InternalMode)0);
		Texture2D texture3 = GD.Load<Texture2D>("res://images/event_extras/spinButton.png");
		float num3 = 256f;
		_buttonRect = new TextureRect
		{
			Texture = texture3,
			CustomMinimumSize = new Vector2(512f, 512f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			PivotOffset = new Vector2(num3, num3),
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)0,
			Visible = false
		};
		((GodotObject)_buttonRect).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)delegate(InputEvent ev)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Invalid comparison between Unknown and I8
			if (!_spinning)
			{
				InputEventMouseButton val5 = (InputEventMouseButton)(object)((ev is InputEventMouseButton) ? ev : null);
				if (val5 != null && val5.Pressed && (long)val5.ButtonIndex == 1)
				{
					((Control)_buttonRect).AcceptEvent();
					StartSpinning();
				}
			}
		}), 0u);
		((GodotObject)_buttonRect).Connect(SignalName.MouseEntered, Callable.From((Action)delegate
		{
			SetButtonHovered(hovered: true);
		}), 0u);
		((GodotObject)_buttonRect).Connect(SignalName.MouseExited, Callable.From((Action)delegate
		{
			SetButtonHovered(hovered: false);
		}), 0u);
		((Node)this).AddChild((Node)(object)_buttonRect, false, (InternalMode)0);
		_buttonGlowRect = new TextureRect
		{
			Texture = texture3,
			CustomMinimumSize = new Vector2(512f, 512f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			PivotOffset = new Vector2(num3, num3),
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2,
			Material = (Material)new CanvasItemMaterial
			{
				BlendMode = (BlendModeEnum)1
			},
			Visible = false
		};
		((Node)this).AddChild((Node)(object)_buttonGlowRect, false, (InternalMode)0);
		PackedScene val3 = GD.Load<PackedScene>("res://scenes/ui/proceed_button.tscn");
		if (val3 != null)
		{
			_controllerButton = val3.Instantiate<NProceedButton>((GenEditState)0);
			((CanvasItem)_controllerButton).Modulate = Colors.Transparent;
			((Node)this).AddChild((Node)(object)_controllerButton, false, (InternalMode)0);
			Callable val4 = Callable.From((Action)delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Expected O, but got Unknown
				_controllerButton.UpdateText(new LocString("gameplay_ui", "PROCEED_BUTTON"));
				((NClickableControl)_controllerButton).Disable();
			});
			((Callable)(ref val4)).CallDeferred(Array.Empty<Variant>());
			((GodotObject)_controllerButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
			{
				if (!_spinning && ((CanvasItem)_buttonRect).Visible)
				{
					StartSpinning();
				}
			}), 0u);
		}
		_controllerIcon = new TextureRect
		{
			CustomMinimumSize = new Vector2(128f, 128f),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			Scale = new Vector2(0.5f, 0.5f),
			PivotOffset = new Vector2(64f, 64f),
			MouseFilter = (MouseFilterEnum)2,
			Visible = false
		};
		((Node)this).AddChild((Node)(object)_controllerIcon, false, (InternalMode)0);
		if (NControllerManager.Instance != null)
		{
			((GodotObject)NControllerManager.Instance).Connect(SignalName.MouseDetected, Callable.From((Action)UpdateControllerIcon), 0u);
			((GodotObject)NControllerManager.Instance).Connect(SignalName.ControllerDetected, Callable.From((Action)UpdateControllerIcon), 0u);
		}
		ApplyWheelOffset();
		ApplyButtonPosition();
		((CanvasItem)this).Modulate = new Color(1f, 1f, 1f, 0f);
	}

	private void SetWheelSlideOffset(float value)
	{
		_wheelSlideOffset = value;
		ApplyWheelOffset();
	}

	private void ApplyWheelOffset()
	{
		float num = 512f;
		float num2 = 256f;
		float num3 = 50f + _wheelSlideOffset;
		((Control)_wheelRect).OffsetTop = 0f - num + num3;
		((Control)_wheelRect).OffsetBottom = num + num3;
		((Control)_arrowRect).OffsetTop = 0f - num2 + num3;
		((Control)_arrowRect).OffsetBottom = num2 + num3;
	}

	private void SetButtonY(float y)
	{
		_buttonY = y;
		ApplyButtonPosition();
	}

	private void ApplyButtonPosition()
	{
		float num = 256f;
		float num2 = _buttonY + 50f;
		((Control)_buttonRect).OffsetLeft = -460f - num;
		((Control)_buttonRect).OffsetRight = -460f + num;
		((Control)_buttonRect).OffsetTop = num2 - num;
		((Control)_buttonRect).OffsetBottom = num2 + num;
		((Control)_buttonGlowRect).OffsetLeft = ((Control)_buttonRect).OffsetLeft;
		((Control)_buttonGlowRect).OffsetRight = ((Control)_buttonRect).OffsetRight;
		((Control)_buttonGlowRect).OffsetTop = ((Control)_buttonRect).OffsetTop;
		((Control)_buttonGlowRect).OffsetBottom = ((Control)_buttonRect).OffsetBottom;
		if (_controllerIcon != null)
		{
			float num3 = 80f;
			((Control)_controllerIcon).OffsetLeft = -460f - num - num3 - 64f;
			((Control)_controllerIcon).OffsetRight = -460f - num - num3 + 64f;
			((Control)_controllerIcon).OffsetTop = num2 - 64f;
			((Control)_controllerIcon).OffsetBottom = num2 + 64f;
		}
	}

	private void StartBounceIn()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		_wheelSlideOffset = -600f;
		_buttonY = 900f;
		ApplyWheelOffset();
		ApplyButtonPosition();
		StartParticleSpawner();
		Tween? mainTween = _mainTween;
		if (mainTween != null)
		{
			mainTween.Kill();
		}
		_mainTween = ((Node)this).CreateTween();
		_mainTween.SetParallel(true);
		_mainTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 1.5).From(Variant.op_Implicit(0f)).SetTrans((TransitionType)7)
			.SetEase((EaseType)1);
		_mainTween.TweenMethod(Callable.From<float>((Action<float>)SetWheelSlideOffset), Variant.op_Implicit(-600f), Variant.op_Implicit(0f), 1.5).SetTrans((TransitionType)9).SetEase((EaseType)1);
		_mainTween.SetParallel(false);
		_mainTween.TweenCallback(Callable.From((Action)delegate
		{
			((CanvasItem)_buttonRect).Visible = true;
			((CanvasItem)_buttonGlowRect).Visible = true;
			NProceedButton? controllerButton = _controllerButton;
			if (controllerButton != null)
			{
				((NClickableControl)controllerButton).Enable();
			}
			UpdateControllerIcon();
			StartGlowPulse();
			SlideButtonIn();
		}));
	}

	private void SlideButtonIn()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		Tween? buttonTween = _buttonTween;
		if (buttonTween != null)
		{
			buttonTween.Kill();
		}
		_buttonTween = ((Node)this).CreateTween();
		_buttonTween.TweenMethod(Callable.From<float>((Action<float>)SetButtonY), Variant.op_Implicit(900f), Variant.op_Implicit(330f), 0.6000000238418579).SetTrans((TransitionType)10).SetEase((EaseType)1);
	}

	private void SlideButtonOut()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		Tween? buttonTween = _buttonTween;
		if (buttonTween != null)
		{
			buttonTween.Kill();
		}
		_buttonTween = ((Node)this).CreateTween();
		_buttonTween.TweenMethod(Callable.From<float>((Action<float>)SetButtonY), Variant.op_Implicit(330f), Variant.op_Implicit(900f), 0.4000000059604645).SetTrans((TransitionType)10).SetEase((EaseType)0);
		_buttonTween.TweenCallback(Callable.From((Action)delegate
		{
			((CanvasItem)_buttonRect).Visible = false;
			((CanvasItem)_buttonGlowRect).Visible = false;
		}));
	}

	private void StartSpinning()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		_spinning = true;
		Tween? glowTween = _glowTween;
		if (glowTween != null)
		{
			glowTween.Kill();
		}
		NProceedButton? controllerButton = _controllerButton;
		if (controllerButton != null)
		{
			((NClickableControl)controllerButton).Disable();
		}
		if (_controllerIcon != null)
		{
			((CanvasItem)_controllerIcon).Visible = false;
		}
		SlideButtonOut();
		AFTPModAudio.Play("events", "wheel");
		float resultAngle = _minigame.ResultAngle;
		float num = 3000f;
		Tween? mainTween = _mainTween;
		if (mainTween != null)
		{
			mainTween.Kill();
		}
		_mainTween = ((Node)this).CreateTween();
		_mainTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float angle)
		{
			((Control)_wheelRect).RotationDegrees = 0f - angle + 0f;
		}), Variant.op_Implicit(0f), Variant.op_Implicit(num), 2.0).SetTrans((TransitionType)0);
		_mainTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			((Control)_wheelRect).RotationDegrees = 0f - ElasticLerp(resultAngle, -180f, t) + 0f;
		}), Variant.op_Implicit(1f), Variant.op_Implicit(0f), 3.0).SetTrans((TransitionType)0);
		_mainTween.TweenCallback(Callable.From((Action)delegate
		{
			float num2 = 0f - resultAngle + 0f;
			float rotationDegrees = ((Control)_wheelRect).RotationDegrees;
			float num3 = rotationDegrees - num2;
			((Control)_wheelRect).RotationDegrees = num2;
		}));
		_mainTween.TweenInterval(1.0);
		_mainTween.TweenCallback(Callable.From((Action)StartBounceOut));
	}

	private void StartBounceOut()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		Tween? spawnTween = _spawnTween;
		if (spawnTween != null)
		{
			spawnTween.Kill();
		}
		Tween? mainTween = _mainTween;
		if (mainTween != null)
		{
			mainTween.Kill();
		}
		_mainTween = ((Node)this).CreateTween();
		_mainTween.SetParallel(true);
		_mainTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.800000011920929).SetTrans((TransitionType)7).SetEase((EaseType)0);
		_mainTween.TweenMethod(Callable.From<float>((Action<float>)SetWheelSlideOffset), Variant.op_Implicit(0f), Variant.op_Implicit(-600f), 0.800000011920929).SetTrans((TransitionType)10).SetEase((EaseType)0);
		_mainTween.SetParallel(false);
		_mainTween.TweenCallback(Callable.From((Action)delegate
		{
			_minigame.Complete();
		}));
	}

	private void StartGlowPulse()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		Tween? glowTween = _glowTween;
		if (glowTween != null)
		{
			glowTween.Kill();
		}
		_glowTween = ((Node)this).CreateTween();
		_glowTween.SetLoops(0);
		_glowTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float a)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			((CanvasItem)_buttonGlowRect).Modulate = new Color(1f, 1f, 1f, a);
		}), Variant.op_Implicit(0.07f), Variant.op_Implicit(0.35f), 0.800000011920929).SetTrans((TransitionType)1).SetEase((EaseType)2);
		_glowTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float a)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			((CanvasItem)_buttonGlowRect).Modulate = new Color(1f, 1f, 1f, a);
		}), Variant.op_Implicit(0.35f), Variant.op_Implicit(0.07f), 0.800000011920929).SetTrans((TransitionType)1).SetEase((EaseType)2);
	}

	private void SetButtonHovered(bool hovered)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (_buttonRect == null || _spinning)
		{
			return;
		}
		((Control)_buttonRect).Scale = (hovered ? (Vector2.One * 1.05f) : Vector2.One);
		((Control)_buttonGlowRect).Scale = ((Control)_buttonRect).Scale;
		if (hovered)
		{
			Tween? glowTween = _glowTween;
			if (glowTween != null)
			{
				glowTween.Kill();
			}
			((CanvasItem)_buttonGlowRect).Modulate = new Color(1f, 1f, 1f, 0.25f);
		}
		else
		{
			StartGlowPulse();
		}
	}

	private void UpdateControllerIcon()
	{
		if (_controllerIcon == null)
		{
			return;
		}
		if (!((CanvasItem)_buttonRect).Visible || _spinning)
		{
			((CanvasItem)_controllerIcon).Visible = false;
			return;
		}
		NControllerManager instance = NControllerManager.Instance;
		if (instance == null || !instance.IsUsingController)
		{
			((CanvasItem)_controllerIcon).Visible = false;
			return;
		}
		NInputManager instance2 = NInputManager.Instance;
		Texture2D val = ((instance2 != null) ? instance2.GetHotkeyIcon(StringName.op_Implicit(MegaInput.accept)) : null);
		if (val != null)
		{
			_controllerIcon.Texture = val;
			((CanvasItem)_controllerIcon).Visible = true;
		}
		else
		{
			((CanvasItem)_controllerIcon).Visible = false;
		}
	}

	private void StartParticleSpawner()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		Tween? spawnTween = _spawnTween;
		if (spawnTween != null)
		{
			spawnTween.Kill();
		}
		_spawnTween = ((Node)this).CreateTween();
		_spawnTween.SetLoops(0);
		_spawnTween.TweenCallback(Callable.From((Action)SpawnParticle));
		_spawnTween.TweenInterval(1.0);
	}

	private void SpawnParticle()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = ((Control)this).Size;
		if (!(size.X <= 0f) && !(size.Y <= 0f))
		{
			EventBgParticleEffect eventBgParticleEffect = EventBgParticleEffect.Create(size / 2f);
			((Node)_particleContainer).AddChild((Node)(object)eventBgParticleEffect, false, (InternalMode)0);
		}
	}

	private static float ElasticIn(float a)
	{
		if (a >= 0.99f)
		{
			return 1f;
		}
		if (a <= 0f)
		{
			return 0f;
		}
		float num = 0f - (float)(Math.Pow(2.0, 10f * (a - 1f)) * Math.Sin((a - 1.1f) * 900f * (float)Math.PI / 180f));
		if (a < 0.5f)
		{
			float num2 = a / 0.5f;
			num *= num2 * num2;
		}
		return num;
	}

	private static float ElasticLerp(float from, float to, float t)
	{
		return from + (to - from) * ElasticIn(t);
	}

	private void OnMinigameFinished()
	{
		NOverlayStack.Instance.Remove((IOverlayScreen)(object)this);
	}
}
