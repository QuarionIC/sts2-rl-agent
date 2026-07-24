using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Automaton.AutomatonCode.Piles;
using BaseLib.Patches.Content;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Automaton.AutomatonCode.Vfx;

[GlobalClass]
[ScriptPath("res://AutomatonCode/Vfx/NStashDisplay.cs")]
public class NStashDisplay : NSlotRevealDisplay
{
	public new class MethodName : NSlotRevealDisplay.MethodName
	{
		public new static readonly StringName GetMaxSlots = StringName.op_Implicit("GetMaxSlots");

		public static readonly StringName GetQueueCount = StringName.op_Implicit("GetQueueCount");

		public static readonly StringName GetCardGlobalPosition = StringName.op_Implicit("GetCardGlobalPosition");

		public new static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public new static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public new class PropertyName : NSlotRevealDisplay.PropertyName
	{
		public new static readonly StringName SlotSeparation = StringName.op_Implicit("SlotSeparation");

		public new static readonly StringName PreviewGap = StringName.op_Implicit("PreviewGap");

		public new static readonly StringName PreviewCardScale = StringName.op_Implicit("PreviewCardScale");

		public new static readonly StringName IsActive = StringName.op_Implicit("IsActive");
	}

	public new class SignalName : NSlotRevealDisplay.SignalName
	{
	}

	private const float StashDisplayScale = 0.28f;

	private const string DisplayScenePath = "res://Automaton/scenes/ui/stash_display.tscn";

	private static readonly Dictionary<Player, NStashDisplay> Displays;

	private CombatManager? _combatManager;

	private CardPile? _stashPile;

	private Player? _trackedPlayer;

	protected override float SlotSeparation => -100f;

	protected override float PreviewGap => 0f;

	protected override float PreviewCardScale => 1f;

	protected override bool IsActive
	{
		get
		{
			if (_trackedPlayer != null)
			{
				CombatManager combatManager = _combatManager;
				if (combatManager != null)
				{
					return combatManager.IsInProgress;
				}
				return false;
			}
			return false;
		}
	}

	static NStashDisplay()
	{
		Displays = new Dictionary<Player, NStashDisplay>();
		CombatManager.Instance.CombatEnded += delegate
		{
			foreach (NStashDisplay item in ((IEnumerable<NStashDisplay>)Displays.Values).Where((Func<NStashDisplay, bool>)GodotObject.IsInstanceValid))
			{
				((Node)item).QueueFree();
			}
			Displays.Clear();
		};
	}

	protected override IReadOnlyList<CardModel> GetSlotCards()
	{
		CardPile? stashPile = _stashPile;
		return ((stashPile != null) ? stashPile.Cards.Skip(1).Reverse().ToList() : null) ?? new List<CardModel>();
	}

	protected override int GetMaxSlots()
	{
		CardPile? stashPile = _stashPile;
		return ((stashPile != null) ? stashPile.Cards.Count : 0) - 1;
	}

	protected override CardModel? CreatePreviewModel(IReadOnlyList<CardModel> slotCards)
	{
		CardPile? stashPile = _stashPile;
		if (stashPile == null)
		{
			return null;
		}
		return stashPile.Cards.FirstOrDefault();
	}

	protected override IReadOnlyList<CardModel> GetDirtyCheckCards()
	{
		CardPile? stashPile = _stashPile;
		return ((stashPile != null) ? stashPile.Cards.ToList() : null) ?? new List<CardModel>();
	}

	protected override string BuildCountText(IReadOnlyList<CardModel> slotCards)
	{
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
		CardPile? stashPile = _stashPile;
		defaultInterpolatedStringHandler.AppendFormatted((stashPile != null) ? stashPile.Cards.Count : 0);
		defaultInterpolatedStringHandler.AppendLiteral("/");
		defaultInterpolatedStringHandler.AppendFormatted(5);
		return defaultInterpolatedStringHandler.ToStringAndClear();
	}

	protected override List<CardModel> BuildInspectList()
	{
		CardPile? stashPile = _stashPile;
		return ((stashPile != null) ? stashPile.Cards.Reverse().ToList() : null) ?? new List<CardModel>();
	}

	public static NStashDisplay? GetDisplay(Player owner)
	{
		if (Displays.TryGetValue(owner, out NStashDisplay value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			return value;
		}
		Displays.Remove(owner);
		return null;
	}

	public int GetQueueCount()
	{
		CardPile? stashPile = _stashPile;
		if (stashPile == null)
		{
			return 0;
		}
		return stashPile.Cards.Count;
	}

	public int GetCardIndex(CardModel card)
	{
		CardPile? stashPile = _stashPile;
		if (stashPile == null)
		{
			return -1;
		}
		return ListExtensions.IndexOf<CardModel>(stashPile.Cards, card);
	}

	public Vector2 GetCardGlobalPosition(int pileIndex)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (pileIndex <= 0)
		{
			return PreviewSlot?.CardAnchorGlobal ?? ((Control)this).GlobalPosition;
		}
		return GetSlotGlobalPosition(pileIndex - 1);
	}

	public static bool HasDisplay(Player player)
	{
		NStashDisplay valueOrDefault = Displays.GetValueOrDefault(player);
		if (valueOrDefault != null)
		{
			return GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault);
		}
		return false;
	}

	public static void SetupFor(NCombatRoom combatRoom, Player player)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (LocalContext.IsMe(player) && !HasDisplay(player))
		{
			NEnergyCounter energyCounter = combatRoom.Ui._energyCounter;
			NStashDisplay nStashDisplay = ResourceLoader.Load<PackedScene>("res://Automaton/scenes/ui/stash_display.tscn", (string)null, (CacheMode)1).Instantiate<NStashDisplay>((GenEditState)0);
			nStashDisplay._trackedPlayer = player;
			nStashDisplay.Direction = RevealDirection.Right;
			((Control)nStashDisplay).Scale = Vector2.One * 0.28f;
			GodotTreeExtensions.AddChildSafely((Node)(object)energyCounter, (Node)(object)nStashDisplay);
			((Control)nStashDisplay).Position = ((Control)energyCounter).Position + new Vector2(70f, -120f);
			Displays[player] = nStashDisplay;
			nStashDisplay.SubscribeToStash(player);
			nStashDisplay.Refresh(force: true);
		}
	}

	public static void EnsureFor(Player player)
	{
		if (!HasDisplay(player))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				SetupFor(instance, player);
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_combatManager = CombatManager.Instance;
	}

	private void SubscribeToStash(Player player)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_stashPile = (CardPile?)(object)CustomPiles.GetCustomPile(player.PlayerCombatState, StashPile.Stash);
		if (_stashPile != null)
		{
			_stashPile.CardAdded += OnPileChanged;
			_stashPile.CardRemoved += OnPileChanged;
		}
	}

	private void OnPileChanged(CardModel _)
	{
		Refresh();
	}

	public override void _ExitTree()
	{
		if (_stashPile != null)
		{
			_stashPile.CardAdded -= OnPileChanged;
			_stashPile.CardRemoved -= OnPileChanged;
			_stashPile = null;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal new static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.GetMaxSlots, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetQueueCount, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetCardGlobalPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("pileIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.GetMaxSlots && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int maxSlots = GetMaxSlots();
			ret = VariantUtils.CreateFrom<int>(ref maxSlots);
			return true;
		}
		if ((ref method) == MethodName.GetQueueCount && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int queueCount = GetQueueCount();
			ret = VariantUtils.CreateFrom<int>(ref queueCount);
			return true;
		}
		if ((ref method) == MethodName.GetCardGlobalPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vector2 cardGlobalPosition = GetCardGlobalPosition(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Vector2>(ref cardGlobalPosition);
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
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.GetMaxSlots)
		{
			return true;
		}
		if ((ref method) == MethodName.GetQueueCount)
		{
			return true;
		}
		if ((ref method) == MethodName.GetCardGlobalPosition)
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
		return base.HasGodotClassMethod(in method);
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
		if ((ref name) == PropertyName.PreviewCardScale)
		{
			float slotSeparation = PreviewCardScale;
			value = VariantUtils.CreateFrom<float>(ref slotSeparation);
			return true;
		}
		if ((ref name) == PropertyName.IsActive)
		{
			bool isActive = IsActive;
			value = VariantUtils.CreateFrom<bool>(ref isActive);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal new static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.SlotSeparation, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewGap, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.PreviewCardScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.IsActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
	}
}
