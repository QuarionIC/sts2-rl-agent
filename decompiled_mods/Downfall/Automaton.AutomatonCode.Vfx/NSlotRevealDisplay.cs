using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Downfall.DownfallCode.Nodes;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace Automaton.AutomatonCode.Vfx;

[ScriptPath("res://AutomatonCode/Vfx/NSlotRevealDisplay.cs")]
public abstract class NSlotRevealDisplay : Control
{
	public enum RevealDirection
	{
		Left,
		Right
	}

	public class MethodName : MethodName
	{
		public static readonly StringName GetMaxSlots = StringName.op_Implicit("GetMaxSlots");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName ReleaseAllSlotCards = StringName.op_Implicit("ReleaseAllSlotCards");

		public static readonly StringName ReleasePreviewCard = StringName.op_Implicit("ReleasePreviewCard");

		public static readonly StringName ComputeHomes = StringName.op_Implicit("ComputeHomes");

		public static readonly StringName GetSlotGlobalPosition = StringName.op_Implicit("GetSlotGlobalPosition");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName UpdateHoverReveal = StringName.op_Implicit("UpdateHoverReveal");

		public static readonly StringName SetSlotsRevealed = StringName.op_Implicit("SetSlotsRevealed");

		public static readonly StringName OnRetractFinished = StringName.op_Implicit("OnRetractFinished");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName SlotSeparation = StringName.op_Implicit("SlotSeparation");

		public static readonly StringName PreviewGap = StringName.op_Implicit("PreviewGap");

		public static readonly StringName RevealDuration = StringName.op_Implicit("RevealDuration");

		public static readonly StringName RevealFadeDuration = StringName.op_Implicit("RevealFadeDuration");

		public static readonly StringName RevealStagger = StringName.op_Implicit("RevealStagger");

		public static readonly StringName RetractDuration = StringName.op_Implicit("RetractDuration");

		public static readonly StringName RetractFadeDuration = StringName.op_Implicit("RetractFadeDuration");

		public static readonly StringName RetractStagger = StringName.op_Implicit("RetractStagger");

		public static readonly StringName PreviewBobSpeed = StringName.op_Implicit("PreviewBobSpeed");

		public static readonly StringName PreviewBobAmplitude = StringName.op_Implicit("PreviewBobAmplitude");

		public static readonly StringName SlotBobAmplitude = StringName.op_Implicit("SlotBobAmplitude");

		public static readonly StringName RetractGraceTime = StringName.op_Implicit("RetractGraceTime");

		public static readonly StringName PreviewCardScale = StringName.op_Implicit("PreviewCardScale");

		public static readonly StringName Direction = StringName.op_Implicit("Direction");

		public static readonly StringName IsActive = StringName.op_Implicit("IsActive");

		public static readonly StringName IsTweenRunning = StringName.op_Implicit("IsTweenRunning");

		public static readonly StringName _bobSpeeds = StringName.op_Implicit("_bobSpeeds");

		public static readonly StringName _lastBobOffsets = StringName.op_Implicit("_lastBobOffsets");

		public static readonly StringName _slotHomes = StringName.op_Implicit("_slotHomes");

		public static readonly StringName _bobTime = StringName.op_Implicit("_bobTime");

		public static readonly StringName _hiddenPosition = StringName.op_Implicit("_hiddenPosition");

		public static readonly StringName _hoverLostTimer = StringName.op_Implicit("_hoverLostTimer");

		public static readonly StringName _initialized = StringName.op_Implicit("_initialized");

		public static readonly StringName _lastPreviewBob = StringName.op_Implicit("_lastPreviewBob");

		public static readonly StringName _previewHolder = StringName.op_Implicit("_previewHolder");

		public static readonly StringName _revealTween = StringName.op_Implicit("_revealTween");

		public static readonly StringName _slotsRevealed = StringName.op_Implicit("_slotsRevealed");

		public static readonly StringName CountLabel = StringName.op_Implicit("CountLabel");

		public static readonly StringName CurrentMax = StringName.op_Implicit("CurrentMax");

		public static readonly StringName PreviewSlot = StringName.op_Implicit("PreviewSlot");
	}

	public class SignalName : SignalName
	{
	}

	private readonly float[] _bobSpeeds = new float[4] { 1.1f, 0.9f, 1.05f, 0.95f };

	private readonly float[] _lastBobOffsets = new float[4];

	private readonly Vector2[] _slotHomes = (Vector2[])(object)new Vector2[4];

	protected readonly List<NCustomCardHolder> CardHolders = new List<NCustomCardHolder>();

	protected readonly List<NAutomatonSlot> Slots = new List<NAutomatonSlot>();

	private float _bobTime;

	private Vector2 _hiddenPosition;

	private float _hoverLostTimer;

	private bool _initialized;

	private List<CardModel> _lastDirtyCards = new List<CardModel>();

	private float _lastPreviewBob;

	private NCustomCardHolder? _previewHolder;

	private Tween? _revealTween;

	private bool _slotsRevealed;

	protected Label? CountLabel;

	protected int CurrentMax = 3;

	protected CardModel? PreviewModel;

	protected NAutomatonSlot? PreviewSlot;

	protected virtual float SlotSeparation => 70f;

	protected virtual float PreviewGap => 160f;

	protected virtual float RevealDuration => 0.25f;

	protected virtual float RevealFadeDuration => 0.18f;

	protected virtual float RevealStagger => 0.05f;

	protected virtual float RetractDuration => 0.2f;

	protected virtual float RetractFadeDuration => 0.15f;

	protected virtual float RetractStagger => 0.04f;

	protected virtual float PreviewBobSpeed => 0.85f;

	protected virtual float PreviewBobAmplitude => 15f;

	protected virtual float SlotBobAmplitude => 15f;

	protected virtual float RetractGraceTime => 0.5f;

	protected virtual float PreviewCardScale => 1.5f;

	[Export(/*Could not decode attribute arguments.*/)]
	public RevealDirection Direction { get; set; }

	protected virtual bool IsActive => true;

	protected bool IsTweenRunning
	{
		get
		{
			if (_revealTween != null && _revealTween.IsValid())
			{
				return _revealTween.IsRunning();
			}
			return false;
		}
	}

	protected abstract IReadOnlyList<CardModel> GetSlotCards();

	protected abstract int GetMaxSlots();

	protected abstract CardModel? CreatePreviewModel(IReadOnlyList<CardModel> slotCards);

	protected virtual IReadOnlyList<CardModel> GetDirtyCheckCards()
	{
		return GetSlotCards();
	}

	protected virtual string BuildCountText(IReadOnlyList<CardModel> slotCards)
	{
		return $"{Math.Min(slotCards.Count, CurrentMax)}/{CurrentMax}";
	}

	protected virtual void OnSlotCardSet(int index, CardModel model, NCard node, NCustomCardHolder holder)
	{
	}

	protected virtual void OnSlotCardCleared(CardModel model)
	{
	}

	protected virtual void OnPreviewCardSet(CardModel model, NCard node, NCustomCardHolder holder)
	{
	}

	protected virtual List<CardModel> BuildInspectList()
	{
		List<CardModel> list = (from h in CardHolders
			where ((NCardHolder)h).CardModel != null
			select ((NCardHolder)h).CardModel).ToList();
		if (PreviewModel != null)
		{
			list.Add(PreviewModel);
		}
		return list;
	}

	public override void _Ready()
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		Slots.Add(((Node)this).GetNode<NAutomatonSlot>(NodePath.op_Implicit("%Slot0")));
		Slots.Add(((Node)this).GetNode<NAutomatonSlot>(NodePath.op_Implicit("%Slot1")));
		Slots.Add(((Node)this).GetNode<NAutomatonSlot>(NodePath.op_Implicit("%Slot2")));
		Slots.Add(((Node)this).GetNode<NAutomatonSlot>(NodePath.op_Implicit("%Slot3")));
		PreviewSlot = ((Node)this).GetNode<NAutomatonSlot>(NodePath.op_Implicit("%FuncPreview"));
		CountLabel = ((Node)this).GetNodeOrNull<Label>(NodePath.op_Implicit("%Count"));
		_hiddenPosition = ((Control)Slots[0]).Position;
		foreach (NAutomatonSlot slot in Slots)
		{
			((CanvasItem)slot).Visible = false;
			((CanvasItem)slot).Modulate = new Color(1f, 1f, 1f, 0f);
		}
		ComputeHomes();
	}

	public override void _ExitTree()
	{
		ReleaseAllSlotCards();
		ReleasePreviewCard();
		Tween? revealTween = _revealTween;
		if (revealTween != null)
		{
			revealTween.Kill();
		}
		_revealTween = null;
	}

	private void ReleaseCardNode(CardModel? model, NCard? cardNode)
	{
		if (model != null)
		{
			OnSlotCardCleared(model);
		}
		if (cardNode != null && GodotObject.IsInstanceValid((GodotObject)(object)cardNode) && ((Node)cardNode).IsInsideTree() && ((Node)this).IsAncestorOf((Node)(object)cardNode))
		{
			Node parent = ((Node)cardNode).GetParent();
			if (parent != null)
			{
				parent.RemoveChild((Node)(object)cardNode);
			}
			((Node)cardNode).QueueFree();
		}
	}

	protected void ReleaseAllSlotCards()
	{
		foreach (NCustomCardHolder cardHolder in CardHolders)
		{
			ReleaseCardNode(((NCardHolder)cardHolder).CardModel, ((NCardHolder)cardHolder).CardNode);
		}
		CardHolders.Clear();
	}

	private void ReleasePreviewCard()
	{
		NCustomCardHolder? previewHolder = _previewHolder;
		ReleaseCardNode(null, (previewHolder != null) ? ((NCardHolder)previewHolder).CardNode : null);
		_previewHolder = null;
		PreviewModel = null;
	}

	private void ComputeHomes()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (PreviewSlot != null && Slots.Count != 0)
		{
			Vector2 size = ((Control)Slots[0]).Size;
			float num = ((Control)PreviewSlot).Position.Y + (((Control)PreviewSlot).Size.Y - size.Y) / 2f;
			for (int i = 0; i < Slots.Count && i < _slotHomes.Length; i++)
			{
				int num2 = CurrentMax - i;
				float num3 = ((Direction != RevealDirection.Left) ? (((Control)PreviewSlot).Position.X + ((Control)PreviewSlot).Size.X + PreviewGap + (float)(num2 - 1) * (size.X + SlotSeparation)) : (((Control)PreviewSlot).Position.X - PreviewGap - size.X - (float)(num2 - 1) * (size.X + SlotSeparation)));
				_slotHomes[i] = new Vector2(num3, num);
			}
		}
	}

	public Vector2 GetSlotGlobalPosition(int index)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Clamp(index, 0, Math.Max(CurrentMax - 1, 0));
		if (num >= Slots.Count)
		{
			return ((Control)this).GlobalPosition;
		}
		NAutomatonSlot nAutomatonSlot = Slots[num];
		Vector2 val = nAutomatonSlot.CardAnchorGlobal - ((Control)nAutomatonSlot).GlobalPosition;
		return ((CanvasItem)this).GetGlobalTransform() * _slotHomes[num] + val;
	}

	public void Refresh(bool force = false)
	{
		IReadOnlyList<CardModel> slotCards = GetSlotCards();
		IReadOnlyList<CardModel> dirtyCheckCards = GetDirtyCheckCards();
		int maxSlots = GetMaxSlots();
		bool flag = maxSlots != CurrentMax;
		if (force || flag || !_initialized || !dirtyCheckCards.SequenceEqual(_lastDirtyCards))
		{
			_lastDirtyCards = dirtyCheckCards.ToList();
			_initialized = true;
			if (flag)
			{
				CurrentMax = maxSlots;
				ComputeHomes();
			}
			if (CountLabel != null)
			{
				CountLabel.Text = BuildCountText(slotCards);
			}
			RefreshSlots(slotCards);
			RefreshPreview(slotCards);
		}
	}

	private void RefreshSlots(IReadOnlyList<CardModel> slotCards)
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		ReleaseAllSlotCards();
		foreach (NAutomatonSlot slot in Slots)
		{
			slot.ClearCard();
		}
		for (int i = 0; i < Slots.Count; i++)
		{
			NAutomatonSlot nAutomatonSlot = Slots[i];
			if (i >= CurrentMax)
			{
				((CanvasItem)nAutomatonSlot).Visible = false;
				continue;
			}
			((CanvasItem)nAutomatonSlot).Visible = _slotsRevealed || IsTweenRunning;
			if (i >= slotCards.Count)
			{
				continue;
			}
			NCard val = NCard.Create(slotCards[i], (ModelVisibility)1);
			if (val == null)
			{
				continue;
			}
			NCustomCardHolder nCustomCardHolder = nAutomatonSlot.SetCard(val);
			if (nCustomCardHolder == null)
			{
				((Node)val).QueueFree();
				continue;
			}
			((NCardHolder)nCustomCardHolder).SetClickable(true);
			int captured = i;
			((NCardHolder)nCustomCardHolder).Pressed += (PressedEventHandler)delegate
			{
				NGame instance = NGame.Instance;
				if (instance != null)
				{
					instance.GetInspectCardScreen().Open(BuildInspectList(), captured, false);
				}
			};
			val.UpdateVisuals((PileType)2, (CardPreviewMode)1);
			CardHolders.Add(nCustomCardHolder);
			OnSlotCardSet(i, slotCards[i], val, nCustomCardHolder);
		}
	}

	private void RefreshPreview(IReadOnlyList<CardModel> slotCards)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		ReleasePreviewCard();
		PreviewSlot?.ClearCard();
		PreviewModel = CreatePreviewModel(slotCards);
		if (PreviewModel == null || PreviewSlot == null)
		{
			return;
		}
		NCard val = NCard.Create(PreviewModel, (ModelVisibility)1);
		if (val == null)
		{
			return;
		}
		_previewHolder = PreviewSlot.SetCard(val, PreviewCardScale);
		if (_previewHolder == null)
		{
			((Node)val).QueueFree();
			PreviewModel = null;
			return;
		}
		((NCardHolder)_previewHolder).SetClickable(true);
		((NCardHolder)_previewHolder).Pressed += (PressedEventHandler)delegate
		{
			List<CardModel> list = BuildInspectList();
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.GetInspectCardScreen().Open(list, list.Count - 1, false);
			}
		};
		val.UpdateVisuals((PileType)2, (CardPreviewMode)1);
		OnPreviewCardSet(PreviewModel, val, _previewHolder);
	}

	public override void _Process(double delta)
	{
		if (!IsActive)
		{
			return;
		}
		UpdateHoverReveal((float)delta);
		_bobTime += (float)delta;
		for (int i = 0; i < _bobSpeeds.Length; i++)
		{
			float num = Mathf.Sin(_bobTime * _bobSpeeds[i] * (float)Math.PI) * SlotBobAmplitude;
			if (Mathf.Abs(num - _lastBobOffsets[i]) > 0.05f)
			{
				_lastBobOffsets[i] = num;
				if (i < Slots.Count)
				{
					Slots[i].BobOffset = num;
				}
			}
		}
		if (PreviewSlot != null)
		{
			float num2 = Mathf.Sin(_bobTime * PreviewBobSpeed * (float)Math.PI) * PreviewBobAmplitude;
			if (Mathf.Abs(num2 - _lastPreviewBob) > 0.05f)
			{
				_lastPreviewBob = num2;
				PreviewSlot.BobOffset = num2;
			}
		}
	}

	private void UpdateHoverReveal(float delta)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (PreviewSlot == null)
		{
			return;
		}
		Vector2 globalMousePosition = ((CanvasItem)this).GetGlobalMousePosition();
		Rect2 globalRect = ((Control)PreviewSlot).GetGlobalRect();
		bool flag = ((Rect2)(ref globalRect)).HasPoint(globalMousePosition);
		if (!flag && _slotsRevealed)
		{
			Rect2 val2 = default(Rect2);
			for (int i = 0; i < CurrentMax && i < Slots.Count; i++)
			{
				NAutomatonSlot nAutomatonSlot = Slots[i];
				Vector2 val = ((CanvasItem)this).GetGlobalTransform() * _slotHomes[i];
				Vector2 size = ((Control)nAutomatonSlot).Size;
				Transform2D globalTransform = ((CanvasItem)nAutomatonSlot).GetGlobalTransform();
				((Rect2)(ref val2))._002Ector(val, size * ((Transform2D)(ref globalTransform)).Scale);
				if (((Rect2)(ref val2)).HasPoint(globalMousePosition))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			_hoverLostTimer = 0f;
			SetSlotsRevealed(revealed: true);
		}
		else if (_slotsRevealed)
		{
			_hoverLostTimer += delta;
			if (_hoverLostTimer >= RetractGraceTime)
			{
				_hoverLostTimer = 0f;
				SetSlotsRevealed(revealed: false);
			}
		}
	}

	protected void SetSlotsRevealed(bool revealed)
	{
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (_slotsRevealed == revealed)
		{
			return;
		}
		_slotsRevealed = revealed;
		Tween? revealTween = _revealTween;
		if (revealTween != null)
		{
			revealTween.Kill();
		}
		_revealTween = null;
		int num = Math.Min(Slots.Count, CurrentMax);
		if (num <= 0)
		{
			if (!revealed)
			{
				OnRetractFinished();
			}
			return;
		}
		_revealTween = ((Node)this).CreateTween().SetParallel(true);
		for (int i = 0; i < num; i++)
		{
			NAutomatonSlot nAutomatonSlot = Slots[i];
			if (revealed)
			{
				((CanvasItem)nAutomatonSlot).Visible = true;
				if (((CanvasItem)nAutomatonSlot).Modulate.A <= 0.001f)
				{
					((Control)nAutomatonSlot).Position = _hiddenPosition;
				}
				float num2 = (float)i * RevealStagger;
				_revealTween.TweenProperty((GodotObject)(object)nAutomatonSlot, NodePath.op_Implicit("position"), Variant.op_Implicit(_slotHomes[i]), (double)RevealDuration).SetDelay((double)num2).SetTrans((TransitionType)7)
					.SetEase((EaseType)1);
				_revealTween.TweenProperty((GodotObject)(object)nAutomatonSlot, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), (double)RevealFadeDuration).SetDelay((double)num2);
			}
			else
			{
				float num3 = (float)(CurrentMax - 1 - i) * RetractStagger;
				_revealTween.TweenProperty((GodotObject)(object)nAutomatonSlot, NodePath.op_Implicit("position"), Variant.op_Implicit(_hiddenPosition), (double)RetractDuration).SetDelay((double)num3).SetTrans((TransitionType)7)
					.SetEase((EaseType)0);
				_revealTween.TweenProperty((GodotObject)(object)nAutomatonSlot, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)RetractFadeDuration).SetDelay((double)num3);
			}
		}
		if (!revealed)
		{
			_revealTween.Chain().TweenCallback(Callable.From((Action)OnRetractFinished));
		}
	}

	private void OnRetractFinished()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		foreach (NAutomatonSlot slot in Slots)
		{
			((CanvasItem)slot).Visible = false;
			((Control)slot).Position = _hiddenPosition;
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
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(12)
		{
			new MethodInfo(MethodName.GetMaxSlots, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReleaseAllSlotCards, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReleasePreviewCard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ComputeHomes, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetSlotGlobalPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("force"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateHoverReveal, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetSlotsRevealed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("revealed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnRetractFinished, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.GetMaxSlots && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int maxSlots = GetMaxSlots();
			ret = VariantUtils.CreateFrom<int>(ref maxSlots);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReleaseAllSlotCards && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReleaseAllSlotCards();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReleasePreviewCard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReleasePreviewCard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ComputeHomes && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ComputeHomes();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetSlotGlobalPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vector2 slotGlobalPosition = GetSlotGlobalPosition(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Vector2>(ref slotGlobalPosition);
			return true;
		}
		if ((ref method) == MethodName.Refresh && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Refresh(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateHoverReveal && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateHoverReveal(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetSlotsRevealed && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetSlotsRevealed(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnRetractFinished && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnRetractFinished();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.GetMaxSlots)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName.ReleaseAllSlotCards)
		{
			return true;
		}
		if ((ref method) == MethodName.ReleasePreviewCard)
		{
			return true;
		}
		if ((ref method) == MethodName.ComputeHomes)
		{
			return true;
		}
		if ((ref method) == MethodName.GetSlotGlobalPosition)
		{
			return true;
		}
		if ((ref method) == MethodName.Refresh)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateHoverReveal)
		{
			return true;
		}
		if ((ref method) == MethodName.SetSlotsRevealed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnRetractFinished)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Direction)
		{
			Direction = VariantUtils.ConvertTo<RevealDirection>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._bobTime)
		{
			_bobTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hiddenPosition)
		{
			_hiddenPosition = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverLostTimer)
		{
			_hoverLostTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			_initialized = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lastPreviewBob)
		{
			_lastPreviewBob = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._previewHolder)
		{
			_previewHolder = VariantUtils.ConvertTo<NCustomCardHolder>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._revealTween)
		{
			_revealTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._slotsRevealed)
		{
			_slotsRevealed = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.CountLabel)
		{
			CountLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.CurrentMax)
		{
			CurrentMax = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PreviewSlot)
		{
			PreviewSlot = VariantUtils.ConvertTo<NAutomatonSlot>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.SlotSeparation)
		{
			float slotSeparation = SlotSeparation;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.PreviewGap)
		{
			float slotSeparation = PreviewGap;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RevealDuration)
		{
			float slotSeparation = RevealDuration;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RevealFadeDuration)
		{
			float slotSeparation = RevealFadeDuration;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RevealStagger)
		{
			float slotSeparation = RevealStagger;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RetractDuration)
		{
			float slotSeparation = RetractDuration;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RetractFadeDuration)
		{
			float slotSeparation = RetractFadeDuration;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RetractStagger)
		{
			float slotSeparation = RetractStagger;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.PreviewBobSpeed)
		{
			float slotSeparation = PreviewBobSpeed;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.PreviewBobAmplitude)
		{
			float slotSeparation = PreviewBobAmplitude;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.SlotBobAmplitude)
		{
			float slotSeparation = SlotBobAmplitude;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.RetractGraceTime)
		{
			float slotSeparation = RetractGraceTime;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.PreviewCardScale)
		{
			float slotSeparation = PreviewCardScale;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.Direction)
		{
			RevealDirection direction = Direction;
			value = VariantUtils.CreateFrom<RevealDirection>(ref direction);
			return true;
		}
		if ((ref name) == PropertyName.IsActive)
		{
			bool isActive = IsActive;
			value = VariantUtils.CreateFrom<bool>(ref isActive);
			return true;
		}
		if ((ref name) == PropertyName.IsTweenRunning)
		{
			bool isActive = IsTweenRunning;
			value = VariantUtils.CreateFrom<bool>(ref isActive);
			return true;
		}
		if ((ref name) == PropertyName._bobSpeeds)
		{
			value = VariantUtils.CreateFrom<float[]>(ref _bobSpeeds);
			return true;
		}
		if ((ref name) == PropertyName._lastBobOffsets)
		{
			value = VariantUtils.CreateFrom<float[]>(ref _lastBobOffsets);
			return true;
		}
		if ((ref name) == PropertyName._slotHomes)
		{
			value = VariantUtils.CreateFrom<Vector2[]>(ref _slotHomes);
			return true;
		}
		if ((ref name) == PropertyName._bobTime)
		{
			value = VariantUtils.CreateFrom<float>(ref _bobTime);
			return true;
		}
		if ((ref name) == PropertyName._hiddenPosition)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _hiddenPosition);
			return true;
		}
		if ((ref name) == PropertyName._hoverLostTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _hoverLostTimer);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			value = VariantUtils.CreateFrom<bool>(ref _initialized);
			return true;
		}
		if ((ref name) == PropertyName._lastPreviewBob)
		{
			value = VariantUtils.CreateFrom<float>(ref _lastPreviewBob);
			return true;
		}
		if ((ref name) == PropertyName._previewHolder)
		{
			value = VariantUtils.CreateFrom<NCustomCardHolder>(ref _previewHolder);
			return true;
		}
		if ((ref name) == PropertyName._revealTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _revealTween);
			return true;
		}
		if ((ref name) == PropertyName._slotsRevealed)
		{
			value = VariantUtils.CreateFrom<bool>(ref _slotsRevealed);
			return true;
		}
		if ((ref name) == PropertyName.CountLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref CountLabel);
			return true;
		}
		if ((ref name) == PropertyName.CurrentMax)
		{
			value = VariantUtils.CreateFrom<int>(ref CurrentMax);
			return true;
		}
		if ((ref name) == PropertyName.PreviewSlot)
		{
			value = VariantUtils.CreateFrom<NAutomatonSlot>(ref PreviewSlot);
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
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)32, PropertyName._bobSpeeds, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)32, PropertyName._lastBobOffsets, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)35, PropertyName._slotHomes, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._bobTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._hiddenPosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._hoverLostTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._initialized, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._lastPreviewBob, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._previewHolder, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._revealTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._slotsRevealed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.CountLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.CurrentMax, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.PreviewSlot, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.SlotSeparation, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewGap, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RevealDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RevealFadeDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RevealStagger, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RetractDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RetractFadeDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RetractStagger, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewBobSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewBobAmplitude, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.SlotBobAmplitude, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RetractGraceTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewCardScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.Direction, (PropertyHint)2, "Left,Right", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.IsActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.IsTweenRunning, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName direction = PropertyName.Direction;
		RevealDirection direction2 = Direction;
		info.AddProperty(direction, Variant.From<RevealDirection>(ref direction2));
		info.AddProperty(PropertyName._bobTime, Variant.From<float>(ref _bobTime));
		info.AddProperty(PropertyName._hiddenPosition, Variant.From<Vector2>(ref _hiddenPosition));
		info.AddProperty(PropertyName._hoverLostTimer, Variant.From<float>(ref _hoverLostTimer));
		info.AddProperty(PropertyName._initialized, Variant.From<bool>(ref _initialized));
		info.AddProperty(PropertyName._lastPreviewBob, Variant.From<float>(ref _lastPreviewBob));
		info.AddProperty(PropertyName._previewHolder, Variant.From<NCustomCardHolder>(ref _previewHolder));
		info.AddProperty(PropertyName._revealTween, Variant.From<Tween>(ref _revealTween));
		info.AddProperty(PropertyName._slotsRevealed, Variant.From<bool>(ref _slotsRevealed));
		info.AddProperty(PropertyName.CountLabel, Variant.From<Label>(ref CountLabel));
		info.AddProperty(PropertyName.CurrentMax, Variant.From<int>(ref CurrentMax));
		info.AddProperty(PropertyName.PreviewSlot, Variant.From<NAutomatonSlot>(ref PreviewSlot));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Direction, ref val))
		{
			Direction = ((Variant)(ref val)).As<RevealDirection>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._bobTime, ref val2))
		{
			_bobTime = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._hiddenPosition, ref val3))
		{
			_hiddenPosition = ((Variant)(ref val3)).As<Vector2>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverLostTimer, ref val4))
		{
			_hoverLostTimer = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._initialized, ref val5))
		{
			_initialized = ((Variant)(ref val5)).As<bool>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._lastPreviewBob, ref val6))
		{
			_lastPreviewBob = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._previewHolder, ref val7))
		{
			_previewHolder = ((Variant)(ref val7)).As<NCustomCardHolder>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._revealTween, ref val8))
		{
			_revealTween = ((Variant)(ref val8)).As<Tween>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._slotsRevealed, ref val9))
		{
			_slotsRevealed = ((Variant)(ref val9)).As<bool>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName.CountLabel, ref val10))
		{
			CountLabel = ((Variant)(ref val10)).As<Label>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName.CurrentMax, ref val11))
		{
			CurrentMax = ((Variant)(ref val11)).As<int>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName.PreviewSlot, ref val12))
		{
			PreviewSlot = ((Variant)(ref val12)).As<NAutomatonSlot>();
		}
	}
}
