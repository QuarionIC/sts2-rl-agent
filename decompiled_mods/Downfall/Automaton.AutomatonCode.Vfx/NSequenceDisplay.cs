using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using Automaton.AutomatonCode.Extensions;
using Automaton.AutomatonCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Downfall.DownfallCode.Nodes;
using Downfall.DownfallCode.Patches;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Automaton.AutomatonCode.Vfx;

[GlobalClass]
[ScriptPath("res://AutomatonCode/Vfx/NSequenceDisplay.cs")]
public class NSequenceDisplay : NSlotRevealDisplay
{
	public new class MethodName : NSlotRevealDisplay.MethodName
	{
		public new static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public new static readonly StringName GetMaxSlots = StringName.op_Implicit("GetMaxSlots");

		public new static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public new class PropertyName : NSlotRevealDisplay.PropertyName
	{
		public new static readonly StringName IsActive = StringName.op_Implicit("IsActive");
	}

	public new class SignalName : NSlotRevealDisplay.SignalName
	{
	}

	private const float SequencedCardScale = 0.28f;

	private const string DisplayScenePath = "res://Automaton/scenes/ui/automaton_display.tscn";

	private static readonly Dictionary<Player, NSequenceDisplay> Displays;

	private CombatManager? _combatManager;

	private Player? _trackedPlayer;

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

	static NSequenceDisplay()
	{
		Displays = new Dictionary<Player, NSequenceDisplay>();
		CombatManager.Instance.CombatEnded += delegate
		{
			foreach (NSequenceDisplay item in ((IEnumerable<NSequenceDisplay>)Displays.Values).Where((Func<NSequenceDisplay, bool>)GodotObject.IsInstanceValid))
			{
				item.ReleaseAllSlotCards();
				((Node)item).QueueFree();
			}
			Displays.Clear();
		};
	}

	public override void _Ready()
	{
		base._Ready();
		_combatManager = CombatManager.Instance;
	}

	protected override IReadOnlyList<CardModel> GetSlotCards()
	{
		return _trackedPlayer?.GetEncode() ?? Array.Empty<CardModel>();
	}

	protected override int GetMaxSlots()
	{
		if (_trackedPlayer != null)
		{
			return AutomatonCmd.GetMax(_trackedPlayer);
		}
		return 3;
	}

	protected override CardModel? CreatePreviewModel(IReadOnlyList<CardModel> slotCards)
	{
		if (_trackedPlayer == null)
		{
			return null;
		}
		if (!(((CardModel)ModelDb.Card<FunctionCard>()).ToMutable() is FunctionCard functionCard))
		{
			return null;
		}
		if (slotCards.Count > 0)
		{
			functionCard.SetSourceCards(slotCards);
		}
		((CardModel)functionCard).Owner = _trackedPlayer;
		IEnumerable<IModifyCompiledFunction> modifiers;
		return (CardModel?)(object)AutomatonHook.ModifyCompiledFunction(_trackedPlayer.Creature.CombatState, functionCard, _trackedPlayer, out modifiers);
	}

	protected override void OnSlotCardSet(int index, CardModel model, NCard node, NCustomCardHolder holder)
	{
		FindOnTablePatch.Register(model, node);
	}

	protected override void OnSlotCardCleared(CardModel model)
	{
		FindOnTablePatch.Unregister(model);
	}

	protected override List<CardModel> BuildInspectList()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Player? trackedPlayer = _trackedPlayer;
		CustomPile customPile = CustomPiles.GetCustomPile((trackedPlayer != null) ? trackedPlayer.PlayerCombatState : null, EncodePile.FunctionSequence);
		List<CardModel> list = (((customPile != null) ? ((CardPile)customPile).Cards : null) ?? Array.Empty<CardModel>()).Concat(from h in CardHolders
			where ((NCardHolder)h).CardModel != null
			select ((NCardHolder)h).CardModel).ToList();
		if (PreviewModel != null)
		{
			list.Add(PreviewModel);
		}
		return list;
	}

	public static NSequenceDisplay? GetDisplay(Player player)
	{
		NSequenceDisplay valueOrDefault = Displays.GetValueOrDefault(player);
		if (valueOrDefault != null && GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			return valueOrDefault;
		}
		Displays.Remove(player);
		return null;
	}

	public static bool HasDisplay(Player player)
	{
		NSequenceDisplay valueOrDefault = Displays.GetValueOrDefault(player);
		if (valueOrDefault != null)
		{
			return GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault);
		}
		return false;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_trackedPlayer != null && Displays.GetValueOrDefault(_trackedPlayer) == this)
		{
			Displays.Remove(_trackedPlayer);
		}
	}

	public static void SetupFor(NCombatRoom combatRoom, Player player)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		if (!HasDisplay(player))
		{
			NSequenceDisplay nSequenceDisplay = ResourceLoader.Load<PackedScene>("res://Automaton/scenes/ui/automaton_display.tscn", (string)null, (CacheMode)1).Instantiate<NSequenceDisplay>((GenEditState)0);
			nSequenceDisplay._trackedPlayer = player;
			((Control)nSequenceDisplay).Scale = Vector2.One * (LocalContext.IsMe(player) ? 0.28f : 0.14f);
			nSequenceDisplay.Direction = RevealDirection.Right;
			((CanvasItem)nSequenceDisplay).ZIndex = (LocalContext.IsMe(player) ? 1 : 0);
			Control combatVfxContainer = combatRoom.CombatVfxContainer;
			GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)nSequenceDisplay);
			NCreature creatureNode = combatRoom.GetCreatureNode(player.Creature);
			if (creatureNode != null)
			{
				Vector2 topOfHitbox = creatureNode.GetTopOfHitbox();
				Transform2D globalTransform = ((CanvasItem)combatVfxContainer).GetGlobalTransform();
				Vector2 val = ((Transform2D)(ref globalTransform)).AffineInverse() * topOfHitbox;
				int num = (LocalContext.IsMe(player) ? (-90) : (-50));
				int num2 = (LocalContext.IsMe(player) ? (-100) : (-40));
				((Control)nSequenceDisplay).Position = val + new Vector2((float)num, (float)num2);
			}
			Displays[player] = nSequenceDisplay;
			((NSlotRevealDisplay)nSequenceDisplay).Refresh(force: true);
		}
	}

	public static void Refresh(Player player, bool force = false)
	{
		if (!HasDisplay(player))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				SetupFor(instance, player);
			}
		}
		else
		{
			((NSlotRevealDisplay)Displays[player]).Refresh(force);
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
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetMaxSlots, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetMaxSlots && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int maxSlots = GetMaxSlots();
			ret = VariantUtils.CreateFrom<int>(ref maxSlots);
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
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.GetMaxSlots)
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
		return new List<PropertyInfo>
		{
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
