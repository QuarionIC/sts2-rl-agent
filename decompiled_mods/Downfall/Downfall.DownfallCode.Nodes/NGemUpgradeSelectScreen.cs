using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Nodes;

[GlobalClass]
[ScriptPath("res://GuardianCode/Vfx/NGemUpgradeSelectScreen.cs")]
public class NGemUpgradeSelectScreen : Control, IOverlayScreen, IScreenContext, ICardSelector
{
	public class MethodName : MethodName
	{
		public static readonly StringName AfterOverlayShown = StringName.op_Implicit("AfterOverlayShown");

		public static readonly StringName AfterOverlayHidden = StringName.op_Implicit("AfterOverlayHidden");

		public static readonly StringName AfterOverlayOpened = StringName.op_Implicit("AfterOverlayOpened");

		public static readonly StringName AfterOverlayClosed = StringName.op_Implicit("AfterOverlayClosed");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName ConnectSignalsAndInitGrid = StringName.op_Implicit("ConnectSignalsAndInitGrid");

		public static readonly StringName RefreshConfirmButtonVisibility = StringName.op_Implicit("RefreshConfirmButtonVisibility");

		public static readonly StringName ConfirmSelection = StringName.op_Implicit("ConfirmSelection");

		public static readonly StringName CheckIfSelectionComplete = StringName.op_Implicit("CheckIfSelectionComplete");

		public static readonly StringName CloseSelection = StringName.op_Implicit("CloseSelection");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName SetPeekButtonTargets = StringName.op_Implicit("SetPeekButtonTargets");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName ScreenType = StringName.op_Implicit("ScreenType");

		public static readonly StringName DefaultFocusedControl = StringName.op_Implicit("DefaultFocusedControl");

		public static readonly StringName FocusedControlFromTopBar = StringName.op_Implicit("FocusedControlFromTopBar");

		public static readonly StringName UseSharedBackstop = StringName.op_Implicit("UseSharedBackstop");

		public static readonly StringName _closeButton = StringName.op_Implicit("_closeButton");

		public static readonly StringName _confirmButton = StringName.op_Implicit("_confirmButton");

		public static readonly StringName _grid = StringName.op_Implicit("_grid");

		public static readonly StringName _infoLabel = StringName.op_Implicit("_infoLabel");

		public static readonly StringName _isSelectingGem = StringName.op_Implicit("_isSelectingGem");

		public static readonly StringName _peekButton = StringName.op_Implicit("_peekButton");
	}

	public class SignalName : SignalName
	{
	}

	private readonly HashSet<CardModel> _selectedCards = new HashSet<CardModel>();

	public readonly TaskCompletionSource<IEnumerable<CardModel>> CompletionSource = new TaskCompletionSource<IEnumerable<CardModel>>();

	private IReadOnlyList<CardModel>? _cards;

	private NBackButton? _closeButton;

	private NConfirmButton? _confirmButton;

	private IReadOnlyList<CardModel>? _gems;

	private NCardGrid? _grid;

	private MegaRichTextLabel? _infoLabel;

	private bool _isSelectingGem = true;

	private NPeekButton? _peekButton;

	private CardSelectorPrefs _prefs;

	private CardModel? _selectedGem;

	private static string ScenePath => "res://Guardian/scenes/gem_upgrade_select_screen.tscn";

	public static IEnumerable<string> AssetPaths => new _003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	private IEnumerable<Control> PeekButtonTargets => new global::_003C_003Ez__ReadOnlyArray<Control>((Control[])(object)new Control[2]
	{
		(Control)_closeButton,
		(Control)_confirmButton
	});

	private static bool UsingController
	{
		get
		{
			NControllerManager instance = NControllerManager.Instance;
			if (instance != null)
			{
				return instance.IsUsingController;
			}
			return false;
		}
	}

	public NetScreenType ScreenType => (NetScreenType)8;

	public Control? DefaultFocusedControl
	{
		get
		{
			NCardGrid? grid = _grid;
			if (grid == null)
			{
				return null;
			}
			return grid.DefaultFocusedControl;
		}
	}

	public Control? FocusedControlFromTopBar
	{
		get
		{
			NCardGrid? grid = _grid;
			if (grid == null)
			{
				return null;
			}
			return grid.FocusedControlFromTopBar;
		}
	}

	public bool UseSharedBackstop => true;

	public async Task<IEnumerable<CardModel>> CardsSelected()
	{
		return await CompletionSource.Task;
	}

	public void AfterOverlayShown()
	{
		if (((CardSelectorPrefs)(ref _prefs)).Cancelable && _closeButton != null)
		{
			((NClickableControl)_closeButton).Enable();
		}
	}

	public void AfterOverlayHidden()
	{
		NBackButton? closeButton = _closeButton;
		if (closeButton != null)
		{
			((NClickableControl)closeButton).Disable();
		}
	}

	public virtual void AfterOverlayOpened()
	{
	}

	public virtual void AfterOverlayClosed()
	{
		if (_peekButton != null)
		{
			_peekButton.SetPeeking(false);
			GodotTreeExtensions.QueueFreeSafely((Node)(object)this);
		}
	}

	public override void _Ready()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		_closeButton = ((Node)this).GetNode<NBackButton>(NodePath.op_Implicit("%Close"));
		_confirmButton = ((Node)this).GetNode<NConfirmButton>(NodePath.op_Implicit("%Confirm"));
		((Control)_confirmButton).FocusMode = (FocusModeEnum)2;
		_infoLabel = ((Node)this).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("%BottomLabel"));
		((GodotObject)_closeButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)CloseSelection), 0u);
		((GodotObject)_confirmButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)ConfirmSelection), 0u);
		if (((CardSelectorPrefs)(ref _prefs)).Cancelable)
		{
			((NClickableControl)_closeButton).Enable();
		}
		else
		{
			((NClickableControl)_closeButton).Disable();
		}
		ConnectSignalsAndInitGrid();
		RefreshConfirmButtonVisibility();
		_infoLabel.Text = ((CardSelectorPrefs)(ref _prefs)).Prompt.GetFormattedText();
	}

	private void ConnectSignalsAndInitGrid()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		if (_gems == null)
		{
			return;
		}
		_grid = ((Node)this).GetNode<NCardGrid>(NodePath.op_Implicit("%CardGrid"));
		_peekButton = ((Node)this).GetNode<NPeekButton>(NodePath.op_Implicit("%PeekButton"));
		RefreshGrid(_gems);
		((GodotObject)_grid).Connect(SignalName.HolderPressed, Callable.From<NCardHolder>((Action<NCardHolder>)delegate(NCardHolder h)
		{
			OnCardClicked(h.CardModel);
		}), 0u);
		((GodotObject)_grid).Connect(SignalName.HolderAltPressed, Callable.From<NCardHolder>((Action<NCardHolder>)delegate(NCardHolder h)
		{
			ShowCardDetail(h.CardModel);
		}), 0u);
		_grid.InsetForTopBar();
		((GodotObject)_peekButton).Connect(SignalName.Toggled, Callable.From<NPeekButton>((Action<NPeekButton>)delegate
		{
			if (_peekButton.IsPeeking)
			{
				((Control)this).MouseFilter = (MouseFilterEnum)2;
			}
			else
			{
				((Control)this).MouseFilter = (MouseFilterEnum)0;
				ActiveScreenContext.Instance.Update();
			}
		}), 0u);
		Callable val = Callable.From((Action)SetPeekButtonTargets);
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private void RefreshGrid(IReadOnlyList<CardModel> cardsToShow)
	{
		if (_grid != null)
		{
			List<SortingOrders> list = new List<SortingOrders>(1);
			CollectionsMarshal.SetCount(list, 1);
			CollectionsMarshal.AsSpan(list)[0] = (SortingOrders)8;
			_grid.SetCards(cardsToShow, (PileType)0, list, (Task)null);
		}
	}

	public static NGemUpgradeSelectScreen ShowScreen(IReadOnlyList<CardModel> gems, IReadOnlyList<CardModel> gemHolder, CardSelectorPrefs prefs)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		NGemUpgradeSelectScreen nGemUpgradeSelectScreen = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NGemUpgradeSelectScreen>((GenEditState)0);
		((Node)nGemUpgradeSelectScreen).Name = StringName.op_Implicit("NGemUpgradeSelectScreen");
		nGemUpgradeSelectScreen._gems = gems;
		nGemUpgradeSelectScreen._cards = gemHolder;
		nGemUpgradeSelectScreen._prefs = prefs;
		NOverlayStack instance = NOverlayStack.Instance;
		if (instance != null)
		{
			instance.Push((IOverlayScreen)(object)nGemUpgradeSelectScreen);
		}
		return nGemUpgradeSelectScreen;
	}

	private void RefreshConfirmButtonVisibility()
	{
		if (_confirmButton != null)
		{
			if (_selectedCards.Count == 1)
			{
				((NClickableControl)_confirmButton).Enable();
			}
			else
			{
				((NClickableControl)_confirmButton).Disable();
			}
		}
	}

	private void OnCardClicked(CardModel card)
	{
		if (_grid == null)
		{
			return;
		}
		if (_selectedCards.Contains(card))
		{
			_selectedCards.Remove(card);
			_grid.UnhighlightCard(card);
		}
		else
		{
			foreach (CardModel selectedCard in _selectedCards)
			{
				_grid.UnhighlightCard(selectedCard);
			}
			_selectedCards.Clear();
			_selectedCards.Add(card);
			_grid.HighlightCard(card);
		}
		RefreshConfirmButtonVisibility();
		if (UsingController && _selectedCards.Count == 1)
		{
			ConfirmSelection(null);
		}
	}

	private void ConfirmSelection(NButton b)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		if (_cards == null)
		{
			return;
		}
		if (_isSelectingGem)
		{
			_selectedGem = _selectedCards.First();
			_selectedCards.Clear();
			_isSelectingGem = false;
			RefreshGrid(_cards);
			RefreshConfirmButtonVisibility();
			if (UsingController)
			{
				GrabFirstCardWhenReady(_cards.Count);
			}
			if (_infoLabel != null)
			{
				LocString val = new LocString("gameplay_ui", "GUARDIAN-GEM_SOCKET_SELECT");
				_infoLabel.Text = val.GetFormattedText();
			}
		}
		else
		{
			CheckIfSelectionComplete();
		}
	}

	private async Task GrabFirstCardWhenReady(int expectedCount)
	{
		if (_grid == null || expectedCount == 0)
		{
			return;
		}
		for (int i = 0; i < 30; i++)
		{
			if (!GodotObject.IsInstanceValid((GodotObject)(object)this))
			{
				break;
			}
			if (_grid == null)
			{
				break;
			}
			Control focusedControlFromTopBar = _grid.FocusedControlFromTopBar;
			if (focusedControlFromTopBar != null)
			{
				NodeUtil.TryGrabFocus(focusedControlFromTopBar);
				break;
			}
			await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
		}
	}

	private void CheckIfSelectionComplete()
	{
		if (_selectedCards.Count != 0 && NOverlayStack.Instance != null && _selectedGem != null)
		{
			List<CardModel> result = new List<CardModel>
			{
				_selectedGem,
				_selectedCards.First()
			};
			CompletionSource.SetResult(result);
			NOverlayStack.Instance.Remove((IOverlayScreen)(object)this);
		}
	}

	private void CloseSelection(NButton _)
	{
		if (NOverlayStack.Instance != null)
		{
			CompletionSource.SetResult(Array.Empty<CardModel>());
			NOverlayStack.Instance.Remove((IOverlayScreen)(object)this);
		}
	}

	public override void _ExitTree()
	{
		if (!CompletionSource.Task.IsCompleted)
		{
			CompletionSource.SetCanceled();
		}
	}

	private void SetPeekButtonTargets()
	{
		if (_peekButton != null && _grid != null)
		{
			HashSet<Control> hashSet = new HashSet<Control> { (Control)(object)_grid };
			hashSet.UnionWith(PeekButtonTargets);
			_peekButton.AddTargets(hashSet.ToArray());
		}
	}

	private void ShowCardDetail(CardModel card)
	{
		if (NControllerManager.Instance != null && NGame.Instance != null && _grid != null && !NControllerManager.Instance.IsUsingController)
		{
			IReadOnlyList<CardModel> readOnlyList = (_isSelectingGem ? _gems : _cards);
			if (readOnlyList != null)
			{
				NGame.Instance.GetInspectCardScreen().Open(readOnlyList.ToList(), readOnlyList.ToList().IndexOf(card), _grid.IsShowingUpgrades);
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Expected O, but got Unknown
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(12)
		{
			new MethodInfo(MethodName.AfterOverlayShown, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayHidden, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayOpened, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayClosed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ConnectSignalsAndInitGrid, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshConfirmButtonVisibility, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ConfirmSelection, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("b"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CheckIfSelectionComplete, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CloseSelection, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("_"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetPeekButtonTargets, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AfterOverlayShown && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayShown();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayHidden && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayHidden();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayOpened && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayOpened();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayClosed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayClosed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ConnectSignalsAndInitGrid && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ConnectSignalsAndInitGrid();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshConfirmButtonVisibility && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshConfirmButtonVisibility();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ConfirmSelection && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ConfirmSelection(VariantUtils.ConvertTo<NButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CheckIfSelectionComplete && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CheckIfSelectionComplete();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CloseSelection && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			CloseSelection(VariantUtils.ConvertTo<NButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetPeekButtonTargets && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetPeekButtonTargets();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.AfterOverlayShown)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayHidden)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayOpened)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayClosed)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.ConnectSignalsAndInitGrid)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshConfirmButtonVisibility)
		{
			return true;
		}
		if ((ref method) == MethodName.ConfirmSelection)
		{
			return true;
		}
		if ((ref method) == MethodName.CheckIfSelectionComplete)
		{
			return true;
		}
		if ((ref method) == MethodName.CloseSelection)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName.SetPeekButtonTargets)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._closeButton)
		{
			_closeButton = VariantUtils.ConvertTo<NBackButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._confirmButton)
		{
			_confirmButton = VariantUtils.ConvertTo<NConfirmButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._grid)
		{
			_grid = VariantUtils.ConvertTo<NCardGrid>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._infoLabel)
		{
			_infoLabel = VariantUtils.ConvertTo<MegaRichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isSelectingGem)
		{
			_isSelectingGem = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._peekButton)
		{
			_peekButton = VariantUtils.ConvertTo<NPeekButton>(ref value);
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
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.ScreenType)
		{
			NetScreenType screenType = ScreenType;
			value = VariantUtils.CreateFrom<NetScreenType>(ref screenType);
			return true;
		}
		if ((ref name) == PropertyName.DefaultFocusedControl)
		{
			Control defaultFocusedControl = DefaultFocusedControl;
			value = VariantUtils.CreateFrom<Control>(ref defaultFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName.FocusedControlFromTopBar)
		{
			Control defaultFocusedControl = FocusedControlFromTopBar;
			value = VariantUtils.CreateFrom<Control>(ref defaultFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName.UseSharedBackstop)
		{
			bool useSharedBackstop = UseSharedBackstop;
			value = VariantUtils.CreateFrom<bool>(ref useSharedBackstop);
			return true;
		}
		if ((ref name) == PropertyName._closeButton)
		{
			value = VariantUtils.CreateFrom<NBackButton>(ref _closeButton);
			return true;
		}
		if ((ref name) == PropertyName._confirmButton)
		{
			value = VariantUtils.CreateFrom<NConfirmButton>(ref _confirmButton);
			return true;
		}
		if ((ref name) == PropertyName._grid)
		{
			value = VariantUtils.CreateFrom<NCardGrid>(ref _grid);
			return true;
		}
		if ((ref name) == PropertyName._infoLabel)
		{
			value = VariantUtils.CreateFrom<MegaRichTextLabel>(ref _infoLabel);
			return true;
		}
		if ((ref name) == PropertyName._isSelectingGem)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isSelectingGem);
			return true;
		}
		if ((ref name) == PropertyName._peekButton)
		{
			value = VariantUtils.CreateFrom<NPeekButton>(ref _peekButton);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._closeButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._confirmButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._grid, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._infoLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isSelectingGem, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._peekButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.ScreenType, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.DefaultFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.FocusedControlFromTopBar, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.UseSharedBackstop, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._closeButton, Variant.From<NBackButton>(ref _closeButton));
		info.AddProperty(PropertyName._confirmButton, Variant.From<NConfirmButton>(ref _confirmButton));
		info.AddProperty(PropertyName._grid, Variant.From<NCardGrid>(ref _grid));
		info.AddProperty(PropertyName._infoLabel, Variant.From<MegaRichTextLabel>(ref _infoLabel));
		info.AddProperty(PropertyName._isSelectingGem, Variant.From<bool>(ref _isSelectingGem));
		info.AddProperty(PropertyName._peekButton, Variant.From<NPeekButton>(ref _peekButton));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._closeButton, ref val))
		{
			_closeButton = ((Variant)(ref val)).As<NBackButton>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._confirmButton, ref val2))
		{
			_confirmButton = ((Variant)(ref val2)).As<NConfirmButton>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._grid, ref val3))
		{
			_grid = ((Variant)(ref val3)).As<NCardGrid>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._infoLabel, ref val4))
		{
			_infoLabel = ((Variant)(ref val4)).As<MegaRichTextLabel>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._isSelectingGem, ref val5))
		{
			_isSelectingGem = ((Variant)(ref val5)).As<bool>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._peekButton, ref val6))
		{
			_peekButton = ((Variant)(ref val6)).As<NPeekButton>();
		}
	}
}
