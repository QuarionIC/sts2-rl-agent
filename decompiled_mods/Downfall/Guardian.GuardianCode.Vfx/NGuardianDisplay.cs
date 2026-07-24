using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Downfall.DownfallCode.Nodes;
using Downfall.DownfallCode.Patches;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace Guardian.GuardianCode.Vfx;

[GlobalClass]
[ScriptPath("res://GuardianCode/Vfx/NGuardianDisplay.cs")]
public class NGuardianDisplay : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName ReleaseHolder = StringName.op_Implicit("ReleaseHolder");

		public static readonly StringName ReleaseAllCards = StringName.op_Implicit("ReleaseAllCards");

		public static readonly StringName EnsureSlotCount = StringName.op_Implicit("EnsureSlotCount");

		public static readonly StringName GetSlotGlobalPosition = StringName.op_Implicit("GetSlotGlobalPosition");

		public static readonly StringName RefreshCounters = StringName.op_Implicit("RefreshCounters");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _bobTime = StringName.op_Implicit("_bobTime");

		public static readonly StringName _creatureHitbox = StringName.op_Implicit("_creatureHitbox");

		public static readonly StringName _currentMax = StringName.op_Implicit("_currentMax");

		public static readonly StringName _initialized = StringName.op_Implicit("_initialized");

		public static readonly StringName _slotContainer = StringName.op_Implicit("_slotContainer");

		public static readonly StringName _stasisSlotScene = StringName.op_Implicit("_stasisSlotScene");
	}

	public class SignalName : SignalName
	{
	}

	private const float SequencedCardScale = 1f;

	private const string DisplayScenePath = "res://Guardian/scenes/guardian_display.tscn";

	private const string StasisSlotScenePath = "res://Guardian/scenes/stasis_slot.tscn";

	private readonly List<NCustomCardHolder> _cardHolders = new List<NCustomCardHolder>();

	private readonly List<NStasisSlot> _slots = new List<NStasisSlot>();

	private float _bobTime;

	private Control? _creatureHitbox;

	private int _currentMax = 3;

	private bool _initialized;

	private HBoxContainer? _slotContainer;

	private PackedScene? _stasisSlotScene;

	private Player? _trackedPlayer;

	public static NGuardianDisplay Create(Player player, Control? creatureHitbox)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		NGuardianDisplay nGuardianDisplay = ResourceLoader.Load<PackedScene>("res://Guardian/scenes/guardian_display.tscn", (string)null, (CacheMode)1).Instantiate<NGuardianDisplay>((GenEditState)0);
		nGuardianDisplay._trackedPlayer = player;
		nGuardianDisplay._creatureHitbox = creatureHitbox;
		((Control)nGuardianDisplay).Scale = Vector2.One * 1f;
		return nGuardianDisplay;
	}

	public override void _Ready()
	{
		_slotContainer = ((Node)this).GetNode<HBoxContainer>(NodePath.op_Implicit("%SlotContainer"));
		_stasisSlotScene = ResourceLoader.Load<PackedScene>("res://Guardian/scenes/stasis_slot.tscn", (string)null, (CacheMode)1);
	}

	public override void _ExitTree()
	{
		ReleaseAllCards();
	}

	private void ReleaseHolder(NCustomCardHolder holder)
	{
		if (((NCardHolder)holder).CardModel != null)
		{
			FindOnTablePatch.Unregister(((NCardHolder)holder).CardModel);
		}
		NCard cardNode = ((NCardHolder)holder).CardNode;
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

	private void ReleaseAllCards()
	{
		foreach (NCustomCardHolder cardHolder in _cardHolders)
		{
			ReleaseHolder(cardHolder);
		}
		_cardHolders.Clear();
	}

	private void EnsureSlotCount(int count)
	{
		if (_slotContainer != null && _stasisSlotScene != null)
		{
			while (_slots.Count > count)
			{
				List<NStasisSlot> slots = _slots;
				NStasisSlot nStasisSlot = slots[slots.Count - 1];
				_slots.RemoveAt(_slots.Count - 1);
				((Node)nStasisSlot).QueueFree();
			}
			while (_slots.Count < count)
			{
				NStasisSlot nStasisSlot2 = _stasisSlotScene.Instantiate<NStasisSlot>((GenEditState)0);
				((Node)_slotContainer).AddChild((Node)(object)nStasisSlot2, false, (InternalMode)0);
				_slots.Add(nStasisSlot2);
			}
		}
	}

	public Vector2 GetSlotGlobalPosition(int index)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Clamp(index, 0, _currentMax - 1);
		if (num >= _slots.Count)
		{
			return ((Control)this).GlobalPosition;
		}
		return _slots[num].CardAnchorGlobal;
	}

	public void RefreshCounters()
	{
		if (_trackedPlayer != null)
		{
			List<CardModel> list = _trackedPlayer.GetStasis().ToList();
			for (int i = 0; i < _slots.Count && i < list.Count; i++)
			{
				_slots[i].UpdateCounterDisplay(list[i]);
			}
		}
	}

	public void Refresh()
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		if (_trackedPlayer == null)
		{
			return;
		}
		List<CardModel> list = _trackedPlayer.GetStasis().ToList();
		_currentMax = GuardianCmd.GetMaxStasisSlots(_trackedPlayer);
		_initialized = true;
		ReleaseAllCards();
		foreach (NStasisSlot slot in _slots)
		{
			slot.ClearCard();
		}
		EnsureSlotCount(_currentMax);
		for (int i = 0; i < _slots.Count; i++)
		{
			NStasisSlot nStasisSlot = _slots[i];
			((CanvasItem)nStasisSlot).Visible = i < _currentMax;
			if (i >= _currentMax || i >= list.Count)
			{
				continue;
			}
			NCard val = NCard.Create(list[i], (ModelVisibility)1);
			if (val == null)
			{
				continue;
			}
			NCustomCardHolder nCustomCardHolder = nStasisSlot.SetCard(val);
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
					instance.GetInspectCardScreen().Open(AllCardsForInspect(), captured, false);
				}
			};
			val.UpdateVisuals((PileType)2, (CardPreviewMode)1);
			FindOnTablePatch.Register(list[i], val);
			_cardHolders.Add(nCustomCardHolder);
		}
		DownfallControllerNav.WireChain((IReadOnlyList<Control>)_cardHolders, wrap: true, rtl: true);
		if (_creatureHitbox != null)
		{
			DownfallControllerNav.LinkAbove((IReadOnlyList<Control>)_cardHolders, _creatureHitbox);
		}
		RefreshCounters();
	}

	private List<CardModel> AllCardsForInspect()
	{
		return (from h in _cardHolders
			where ((NCardHolder)h).CardModel != null
			select ((NCardHolder)h).CardModel).ToList();
	}

	public NCard? GetNCard(CardModel card)
	{
		NCustomCardHolder? nCustomCardHolder = _cardHolders.Find((NCustomCardHolder h) => ((NCardHolder)h).CardModel == card);
		NCard val = ((nCustomCardHolder != null) ? ((NCardHolder)nCustomCardHolder).CardNode : null);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && val.Model == card)
		{
			return val;
		}
		return null;
	}

	public Vector2? GetTargetPosition(CardModel card)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (_trackedPlayer == null)
		{
			return ((Control)this).GlobalPosition;
		}
		List<CardModel> list = _trackedPlayer.GetStasis().ToList();
		int num = list.IndexOf(card);
		if (num >= 0)
		{
			return (num < _slots.Count) ? _slots[num].CardAnchorGlobal : ((Control)this).GlobalPosition;
		}
		int num2 = list.Count;
		if (num2 >= _currentMax)
		{
			num2 = _currentMax - 1;
		}
		return (num2 < _slots.Count) ? _slots[num2].CardAnchorGlobal : ((Control)this).GlobalPosition;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(8)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReleaseHolder, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("holder"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ReleaseAllCards, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.EnsureSlotCount, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("count"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetSlotGlobalPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshCounters, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
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
		if ((ref method) == MethodName.ReleaseHolder && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ReleaseHolder(VariantUtils.ConvertTo<NCustomCardHolder>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReleaseAllCards && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReleaseAllCards();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.EnsureSlotCount && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			EnsureSlotCount(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetSlotGlobalPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vector2 slotGlobalPosition = GetSlotGlobalPosition(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Vector2>(ref slotGlobalPosition);
			return true;
		}
		if ((ref method) == MethodName.RefreshCounters && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshCounters();
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
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName.ReleaseHolder)
		{
			return true;
		}
		if ((ref method) == MethodName.ReleaseAllCards)
		{
			return true;
		}
		if ((ref method) == MethodName.EnsureSlotCount)
		{
			return true;
		}
		if ((ref method) == MethodName.GetSlotGlobalPosition)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshCounters)
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
		if ((ref name) == PropertyName._bobTime)
		{
			_bobTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			_creatureHitbox = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentMax)
		{
			_currentMax = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			_initialized = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._slotContainer)
		{
			_slotContainer = VariantUtils.ConvertTo<HBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._stasisSlotScene)
		{
			_stasisSlotScene = VariantUtils.ConvertTo<PackedScene>(ref value);
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
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._bobTime)
		{
			value = VariantUtils.CreateFrom<float>(ref _bobTime);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			value = VariantUtils.CreateFrom<Control>(ref _creatureHitbox);
			return true;
		}
		if ((ref name) == PropertyName._currentMax)
		{
			value = VariantUtils.CreateFrom<int>(ref _currentMax);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			value = VariantUtils.CreateFrom<bool>(ref _initialized);
			return true;
		}
		if ((ref name) == PropertyName._slotContainer)
		{
			value = VariantUtils.CreateFrom<HBoxContainer>(ref _slotContainer);
			return true;
		}
		if ((ref name) == PropertyName._stasisSlotScene)
		{
			value = VariantUtils.CreateFrom<PackedScene>(ref _stasisSlotScene);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._bobTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._creatureHitbox, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._currentMax, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._initialized, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._slotContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._stasisSlotScene, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		info.AddProperty(PropertyName._bobTime, Variant.From<float>(ref _bobTime));
		info.AddProperty(PropertyName._creatureHitbox, Variant.From<Control>(ref _creatureHitbox));
		info.AddProperty(PropertyName._currentMax, Variant.From<int>(ref _currentMax));
		info.AddProperty(PropertyName._initialized, Variant.From<bool>(ref _initialized));
		info.AddProperty(PropertyName._slotContainer, Variant.From<HBoxContainer>(ref _slotContainer));
		info.AddProperty(PropertyName._stasisSlotScene, Variant.From<PackedScene>(ref _stasisSlotScene));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._bobTime, ref val))
		{
			_bobTime = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._creatureHitbox, ref val2))
		{
			_creatureHitbox = ((Variant)(ref val2)).As<Control>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentMax, ref val3))
		{
			_currentMax = ((Variant)(ref val3)).As<int>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._initialized, ref val4))
		{
			_initialized = ((Variant)(ref val4)).As<bool>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._slotContainer, ref val5))
		{
			_slotContainer = ((Variant)(ref val5)).As<HBoxContainer>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._stasisSlotScene, ref val6))
		{
			_stasisSlotScene = ((Variant)(ref val6)).As<PackedScene>();
		}
	}
}
