using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace ActsFromThePast.Minigames;

public class NMatchAndKeepScreen : Control, IOverlayScreen, IScreenContext
{
	private class CardSlot
	{
		public Control Wrapper { get; init; } = null;

		public NGridCardHolder Holder { get; init; } = null;

		public NCard CardNode { get; init; } = null;

		public TextureRect? Overlay { get; init; }

		public int PairIndex { get; init; }

		public int Col { get; init; }

		public int Row { get; init; }

		public bool IsFaceUp { get; set; }

		public bool IsMatched { get; set; }
	}

	private const float GridScale = 0.5f;

	private const float SelectedScale = 0.6f;

	private const float MismatchScale = 0.75f;

	private const float ScaleTweenTime = 0.2f;

	private static readonly float[] ColOffsets = new float[4] { -320f, -110f, 100f, 310f };

	private static readonly float[] RowOffsets = new float[3] { -210f, 20f, 250f };

	private const float MatchWait = 1f;

	private const float MismatchWait = 1.25f;

	private const float GameDoneWait = 1f;

	private const float CleanupWait = 1f;

	private const float CardSlideTime = 0.4f;

	private const float FadeInDuration = 0.5f;

	private const float FadeOutDuration = 0.5f;

	private const string CardBackAtlasPath = "res://images/event_extras/cardui.atlas";

	private const string CardBackRegionName = "512/card_back";

	private const string EventRegionName = "event";

	private const string LocPrefix = "ACTSFROMTHEPAST-MATCH_AND_KEEP.minigame";

	private static NMatchAndKeepScreen? _instance;

	private MatchAndKeepMinigame _minigame = null;

	private Control _particleContainer = null;

	private Control _gridContainer = null;

	private MegaRichTextLabel _attemptsLabel = null;

	private readonly List<CardSlot> _slots = new List<CardSlot>();

	private int _firstSelection = -1;

	private int _attemptsLeft;

	private int _matchCount;

	private bool _isProcessing;

	private Tween? _waitTween;

	private Tween? _spawnTween;

	private AtlasTexture? _cardBackTexture;

	public NetScreenType ScreenType => (NetScreenType)0;

	public bool UseSharedBackstop => false;

	public Control DefaultFocusedControl
	{
		get
		{
			foreach (CardSlot slot in _slots)
			{
				if (!slot.IsMatched)
				{
					return (Control)(object)slot.Holder;
				}
			}
			return (Control)(object)this;
		}
	}

	public static NMatchAndKeepScreen ShowScreen(MatchAndKeepMinigame minigame)
	{
		if (_instance != null && GodotObject.IsInstanceValid((GodotObject)(object)_instance))
		{
			((Node)_instance).QueueFree();
		}
		NMatchAndKeepScreen nMatchAndKeepScreen = new NMatchAndKeepScreen();
		nMatchAndKeepScreen._minigame = minigame;
		nMatchAndKeepScreen._attemptsLeft = minigame.MaxAttempts;
		nMatchAndKeepScreen.LoadCardBack();
		nMatchAndKeepScreen.BuildUI();
		_instance = nMatchAndKeepScreen;
		NOverlayStack.Instance.Push((IOverlayScreen)(object)nMatchAndKeepScreen);
		nMatchAndKeepScreen.SetupFocusNeighbors();
		nMatchAndKeepScreen.DealCards();
		return nMatchAndKeepScreen;
	}

	public override void _ExitTree()
	{
		KillAllTweens();
		_minigame.ForceEnd();
		_instance = null;
	}

	private void KillAllTweens()
	{
		Tween? waitTween = _waitTween;
		if (waitTween != null)
		{
			waitTween.Kill();
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

	private void LoadCardBack()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://images/event_extras/cardui.atlas", "512/card_back");
		if (region.HasValue)
		{
			_cardBackTexture = new AtlasTexture();
			_cardBackTexture.Atlas = region.Value.Texture;
			_cardBackTexture.Region = region.Value.Region;
		}
	}

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

	private void BuildUI()
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Expected O, but got Unknown
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
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
		_gridContainer = new Control
		{
			MouseFilter = (MouseFilterEnum)2
		};
		_gridContainer.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)_gridContainer, false, (InternalMode)0);
		for (int i = 0; i < 12; i++)
		{
			CreateCardSlot(i);
		}
		FontVariation val3 = GD.Load<FontVariation>("res://themes/kreon_bold_glyph_space_one.tres");
		_attemptsLabel = new MegaRichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			MouseFilter = (MouseFilterEnum)2,
			AutowrapMode = (AutowrapMode)0,
			AnchorLeft = 0.5f,
			AnchorTop = 1f,
			AnchorRight = 0.5f,
			AnchorBottom = 1f,
			OffsetLeft = -200f,
			OffsetTop = -80f,
			OffsetRight = 200f,
			OffsetBottom = -30f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)0
		};
		((Control)_attemptsLabel).AddThemeColorOverride(StringName.op_Implicit("default_color"), Colors.White);
		((Control)_attemptsLabel).AddThemeFontSizeOverride(StringName.op_Implicit("normal_font_size"), 30);
		if (val3 != null)
		{
			((Control)_attemptsLabel).AddThemeFontOverride(StringName.op_Implicit("normal_font"), (Font)(object)val3);
		}
		((Control)_attemptsLabel).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 10);
		((Control)_attemptsLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0.15f, 0.1f, 0.23f, 1f));
		((Node)this).AddChild((Node)(object)_attemptsLabel, false, (InternalMode)0);
		RefreshAttemptsLabel();
		StartParticleSpawner();
		((CanvasItem)this).Modulate = new Color(1f, 1f, 1f, 0f);
	}

	private void CreateCardSlot(int index)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected O, but got Unknown
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Expected O, but got Unknown
		int col = index % 4;
		int row = index % 3;
		Control val = new Control
		{
			MouseFilter = (MouseFilterEnum)2,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = 0f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			Scale = new Vector2(0.5f, 0.5f)
		};
		((Node)_gridContainer).AddChild((Node)(object)val, false, (InternalMode)0);
		CardModel val2 = _minigame.Cards[index];
		NCard ncard = NCard.Create(val2, (ModelVisibility)1);
		NGridCardHolder holder = NGridCardHolder.Create(ncard);
		((Control)holder).Position = Vector2.Zero;
		((Node)val).AddChild((Node)(object)holder, false, (InternalMode)0);
		((CanvasItem)ncard).Visible = false;
		Callable val3 = Callable.From((Action)delegate
		{
			ncard.UpdateVisuals((PileType)0, (CardPreviewMode)1);
		});
		((Callable)(ref val3)).CallDeferred(Array.Empty<Variant>());
		TextureRect val4 = null;
		if (_cardBackTexture != null)
		{
			val4 = new TextureRect
			{
				Texture = (Texture2D)(object)_cardBackTexture,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5,
				CustomMinimumSize = new Vector2(300f, 422f),
				Position = new Vector2(-150f, -211f),
				Size = new Vector2(300f, 422f),
				MouseFilter = (MouseFilterEnum)2,
				Visible = true
			};
			((Node)holder).AddChild((Node)(object)val4, false, (InternalMode)0);
		}
		int idx = index;
		((NCardHolder)holder).Pressed += (PressedEventHandler)delegate
		{
			OnCardClicked(idx);
		};
		((GodotObject)holder).Connect(SignalName.MouseEntered, Callable.From((Action)delegate
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			if (idx < _slots.Count && !_slots[idx].IsFaceUp)
			{
				Callable val5 = Callable.From((Action)delegate
				{
					NHoverTipSet.Remove((Control)(object)holder);
				});
				((Callable)(ref val5)).CallDeferred(Array.Empty<Variant>());
			}
		}), 0u);
		((GodotObject)holder).Connect(SignalName.FocusEntered, Callable.From((Action)delegate
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			if (idx < _slots.Count && !_slots[idx].IsFaceUp)
			{
				Callable val5 = Callable.From((Action)delegate
				{
					NHoverTipSet.Remove((Control)(object)holder);
				});
				((Callable)(ref val5)).CallDeferred(Array.Empty<Variant>());
			}
		}), 0u);
		val.OffsetTop = 800f;
		val.OffsetBottom = 800f;
		_slots.Add(new CardSlot
		{
			Wrapper = val,
			Holder = holder,
			CardNode = ncard,
			Overlay = val4,
			PairIndex = _minigame.PairIndices[index],
			IsFaceUp = false,
			IsMatched = false,
			Col = col,
			Row = row
		});
	}

	private void SetupFocusNeighbors()
	{
		int[,] array = new int[4, 3];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				array[i, j] = -1;
			}
		}
		for (int k = 0; k < _slots.Count; k++)
		{
			if (!_slots[k].IsMatched)
			{
				array[_slots[k].Col, _slots[k].Row] = k;
			}
		}
		for (int l = 0; l < _slots.Count; l++)
		{
			CardSlot cardSlot = _slots[l];
			if (!cardSlot.IsMatched)
			{
				NGridCardHolder holder = cardSlot.Holder;
				int col = cardSlot.Col;
				int row = cardSlot.Row;
				NGridCardHolder? obj = FindNeighbor(array, col, row, -1, 0);
				((Control)holder).FocusNeighborLeft = ((obj != null) ? ((Node)obj).GetPath() : null) ?? ((Node)holder).GetPath();
				NGridCardHolder? obj2 = FindNeighbor(array, col, row, 1, 0);
				((Control)holder).FocusNeighborRight = ((obj2 != null) ? ((Node)obj2).GetPath() : null) ?? ((Node)holder).GetPath();
				NGridCardHolder? obj3 = FindNeighbor(array, col, row, 0, -1);
				((Control)holder).FocusNeighborTop = ((obj3 != null) ? ((Node)obj3).GetPath() : null) ?? ((Node)holder).GetPath();
				NGridCardHolder? obj4 = FindNeighbor(array, col, row, 0, 1);
				((Control)holder).FocusNeighborBottom = ((obj4 != null) ? ((Node)obj4).GetPath() : null) ?? ((Node)holder).GetPath();
			}
		}
	}

	private NGridCardHolder? FindNeighbor(int[,] gridMap, int col, int row, int dCol, int dRow)
	{
		int num = col + dCol;
		int num2 = row + dRow;
		for (int i = 0; i < 4; i++)
		{
			if (num < 0)
			{
				num = 3;
			}
			if (num > 3)
			{
				num = 0;
			}
			if (num2 < 0)
			{
				num2 = 2;
			}
			if (num2 > 2)
			{
				num2 = 0;
			}
			int num3 = gridMap[num, num2];
			if (num3 >= 0)
			{
				return _slots[num3].Holder;
			}
			num += dCol;
			num2 += dRow;
		}
		return null;
	}

	private void DealCards()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		Tween val = ((Node)this).CreateTween();
		val.SetParallel(true);
		val.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.5).From(Variant.op_Implicit(0f)).SetTrans((TransitionType)7)
			.SetEase((EaseType)1);
		for (int i = 0; i < _slots.Count; i++)
		{
			CardSlot cardSlot = _slots[i];
			float num = ColOffsets[cardSlot.Col];
			float num2 = RowOffsets[cardSlot.Row];
			val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_left"), Variant.op_Implicit(num), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)1);
			val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(num), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)1);
			val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_top"), Variant.op_Implicit(num2), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)1);
			val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_bottom"), Variant.op_Implicit(num2), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)1);
		}
		val.SetParallel(false);
		val.TweenCallback(Callable.From((Action)delegate
		{
		}));
	}

	private void OnCardClicked(int index)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if (_isProcessing)
		{
			return;
		}
		CardSlot slot = _slots[index];
		if (slot.IsFaceUp || slot.IsMatched)
		{
			return;
		}
		slot.IsFaceUp = true;
		((CanvasItem)slot.CardNode).Visible = true;
		if (slot.Overlay != null)
		{
			((CanvasItem)slot.Overlay).Visible = false;
		}
		Tween val = ((Node)slot.Wrapper).CreateTween();
		val.TweenProperty((GodotObject)(object)slot.Wrapper, NodePath.op_Implicit("scale"), Variant.op_Implicit(new Vector2(0.6f, 0.6f)), 0.20000000298023224).SetTrans((TransitionType)7).SetEase((EaseType)1);
		val.TweenCallback(Callable.From((Action)delegate
		{
			Traverse val2 = Traverse.Create((object)slot.Holder);
			val2.Field("_isFocused").SetValue((object)false);
			val2.Method("RefreshFocusState", Array.Empty<object>()).GetValue();
		}));
		if (_firstSelection < 0)
		{
			_firstSelection = index;
			return;
		}
		int firstSelection = _firstSelection;
		_firstSelection = -1;
		_isProcessing = true;
		if (((AbstractModel)_minigame.Cards[firstSelection]).Id == ((AbstractModel)_minigame.Cards[index]).Id)
		{
			HandleMatch(firstSelection, index);
		}
		else
		{
			HandleMismatch(firstSelection, index);
		}
	}

	private void HandleMatch(int a, int b)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		Tween val = ((Node)this).CreateTween();
		val.SetParallel(true);
		int[] array = new int[2] { a, b };
		foreach (int index in array)
		{
			val.TweenProperty((GodotObject)(object)_slots[index].Wrapper, NodePath.op_Implicit("offset_left"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)_slots[index].Wrapper, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)_slots[index].Wrapper, NodePath.op_Implicit("offset_top"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)_slots[index].Wrapper, NodePath.op_Implicit("offset_bottom"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)2);
		}
		val.SetParallel(false);
		val.TweenInterval(1.0);
		val.TweenCallback(Callable.From((Action)delegate
		{
			_slots[a].IsMatched = true;
			_slots[b].IsMatched = true;
			((CanvasItem)_slots[a].Wrapper).Visible = false;
			((CanvasItem)_slots[b].Wrapper).Visible = false;
			CardModel canonical = _minigame.Canonicals[_slots[a].PairIndex];
			Player player = _minigame.Owner;
			TaskHelper.RunSafely(AddMatchedCard());
			_matchCount++;
			_attemptsLeft--;
			RefreshAttemptsLabel();
			SetupFocusNeighbors();
			_isProcessing = false;
			CheckGameEnd();
			async Task AddMatchedCard()
			{
				CardModel cardInstance = ((ICardScope)player.RunState).CreateCard(canonical, player);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardInstance, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
		}));
	}

	private void HandleMismatch(int a, int b)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		SetSlotScale(a, 0.75f);
		SetSlotScale(b, 0.75f);
		Tween? waitTween = _waitTween;
		if (waitTween != null)
		{
			waitTween.Kill();
		}
		_waitTween = ((Node)this).CreateTween();
		_waitTween.TweenInterval(1.25);
		_waitTween.TweenCallback(Callable.From((Action)delegate
		{
			_slots[a].IsFaceUp = false;
			_slots[b].IsFaceUp = false;
			((CanvasItem)_slots[a].CardNode).Visible = false;
			((CanvasItem)_slots[b].CardNode).Visible = false;
			if (_slots[a].Overlay != null)
			{
				((CanvasItem)_slots[a].Overlay).Visible = true;
			}
			if (_slots[b].Overlay != null)
			{
				((CanvasItem)_slots[b].Overlay).Visible = true;
			}
			SetSlotScale(a, 0.5f);
			SetSlotScale(b, 0.5f);
			_attemptsLeft--;
			RefreshAttemptsLabel();
			_isProcessing = false;
			CheckGameEnd();
		}));
	}

	private void SetSlotScale(int index, float scale)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Control wrapper = _slots[index].Wrapper;
		Tween val = ((Node)wrapper).CreateTween();
		val.TweenProperty((GodotObject)(object)wrapper, NodePath.op_Implicit("scale"), Variant.op_Implicit(new Vector2(scale, scale)), 0.20000000298023224).SetTrans((TransitionType)7).SetEase((EaseType)1);
	}

	private void CheckGameEnd()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		bool flag = _matchCount >= 6;
		bool flag2 = _attemptsLeft <= 0;
		if (!flag && !flag2)
		{
			return;
		}
		_isProcessing = true;
		Tween? waitTween = _waitTween;
		if (waitTween != null)
		{
			waitTween.Kill();
		}
		_waitTween = ((Node)this).CreateTween();
		if (flag2 && !flag)
		{
			_waitTween.TweenInterval(1.0);
		}
		_waitTween.TweenCallback(Callable.From((Action)delegate
		{
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			Tween val = ((Node)this).CreateTween();
			val.SetParallel(true);
			for (int i = 0; i < _slots.Count; i++)
			{
				CardSlot cardSlot = _slots[i];
				if (!cardSlot.IsMatched)
				{
					SetSlotScale(i, 0.5f);
					val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_left"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)0);
					val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)0);
					val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_top"), Variant.op_Implicit(800f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)0);
					val.TweenProperty((GodotObject)(object)cardSlot.Wrapper, NodePath.op_Implicit("offset_bottom"), Variant.op_Implicit(800f), 0.4000000059604645).SetTrans((TransitionType)7).SetEase((EaseType)0);
				}
			}
			val.SetParallel(false);
			val.TweenInterval(1.0);
			val.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.5).SetTrans((TransitionType)7).SetEase((EaseType)0);
			val.TweenCallback(Callable.From((Action)delegate
			{
				Tween? spawnTween = _spawnTween;
				if (spawnTween != null)
				{
					spawnTween.Kill();
				}
				_minigame.Complete();
				NOverlayStack.Instance.Remove((IOverlayScreen)(object)this);
			}));
		}));
	}

	private void RefreshAttemptsLabel()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		LocString val = new LocString("events", "ACTSFROMTHEPAST-MATCH_AND_KEEP.minigame.attempts");
		val.Add("Count", (decimal)_attemptsLeft);
		_attemptsLabel.Text = "[center]" + val.GetFormattedText() + "[/center]";
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
}
