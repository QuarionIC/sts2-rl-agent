using System.Collections.Generic;
using Godot;
using Guardian.GuardianCode.Vfx;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Guardian.GuardianCode.Displays;

public class GuardianDisplay
{
	private static readonly Dictionary<Player, NGuardianDisplay> Displays = new Dictionary<Player, NGuardianDisplay>();

	public static bool HasDisplay(Player player)
	{
		if (Displays.TryGetValue(player, out NGuardianDisplay value))
		{
			return GodotObject.IsInstanceValid((GodotObject)(object)value);
		}
		return false;
	}

	public static void Refresh(Player creature)
	{
		NGuardianDisplay valueOrDefault = Displays.GetValueOrDefault(creature);
		if (GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			valueOrDefault.Refresh();
		}
		else
		{
			Displays.Remove(creature);
		}
	}

	public static void RefreshCounters(Player creature)
	{
		NGuardianDisplay valueOrDefault = Displays.GetValueOrDefault(creature);
		if (GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			valueOrDefault.RefreshCounters();
		}
		else
		{
			Displays.Remove(creature);
		}
	}

	public static void Register(Player creature, NGuardianDisplay display)
	{
		if (Displays.TryGetValue(creature, out NGuardianDisplay value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((Node)value).QueueFree();
		}
		Displays[creature] = display;
	}

	public static NCard? GetNCard(CardModel card)
	{
		NGuardianDisplay valueOrDefault = Displays.GetValueOrDefault(card.Owner);
		if (!GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			return null;
		}
		return valueOrDefault.GetNCard(card);
	}

	public static Vector2? GetPosition(CardModel model)
	{
		NGuardianDisplay valueOrDefault = Displays.GetValueOrDefault(model.Owner);
		if (!GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			return null;
		}
		return valueOrDefault.GetTargetPosition(model);
	}

	public static void SetupGuardianUi(NCombatRoom combatRoom, Player player)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		NCreature creatureNode = combatRoom.GetCreatureNode(player.Creature);
		NGuardianDisplay nGuardianDisplay = NGuardianDisplay.Create(player, (creatureNode != null) ? creatureNode.Hitbox : null);
		Control combatVfxContainer = combatRoom.CombatVfxContainer;
		GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)nGuardianDisplay);
		if (creatureNode != null)
		{
			Vector2 topOfHitbox = creatureNode.GetTopOfHitbox();
			Transform2D globalTransform = ((CanvasItem)combatVfxContainer).GetGlobalTransform();
			((Control)nGuardianDisplay).Position = ((Transform2D)(ref globalTransform)).AffineInverse() * topOfHitbox;
			((Control)nGuardianDisplay).Position = ((Control)nGuardianDisplay).Position + new Vector2(0f, -120f);
		}
		Register(player, nGuardianDisplay);
	}
}
