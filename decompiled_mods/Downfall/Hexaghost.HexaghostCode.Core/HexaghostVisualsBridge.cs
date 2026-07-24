using System.Collections.Generic;
using Godot;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Hexaghost.HexaghostCode.Core;

public static class HexaghostVisualsBridge
{
	private const string GhostflamesScenePath = "res://Hexaghost/scenes/ui/ghostflames.tscn";

	private static readonly Dictionary<Player, NGhostflames> Displays = new Dictionary<Player, NGhostflames>();

	public static NGhostflames? GetVisuals(Player player)
	{
		NGhostflames valueOrDefault = Displays.GetValueOrDefault(player);
		if (GodotObject.IsInstanceValid((GodotObject)(object)valueOrDefault))
		{
			return valueOrDefault;
		}
		Displays.Remove(player);
		return null;
	}

	public static void DiscardDisplay(Player player)
	{
		if (Displays.TryGetValue(player, out NGhostflames value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((Node)value).QueueFree();
		}
		Displays.Remove(player);
	}

	public static void Setup(NCombatRoom combatRoom, Player player)
	{
		if (Displays.TryGetValue(player, out NGhostflames value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((Node)value).QueueFree();
		}
		NGhostflames nGhostflames = ResourceLoader.Load<PackedScene>("res://Hexaghost/scenes/ui/ghostflames.tscn", (string)null, (CacheMode)1).Instantiate<NGhostflames>((GenEditState)0);
		Control combatVfxContainer = combatRoom.CombatVfxContainer;
		GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)nGhostflames);
		NCreature creatureNode = combatRoom.GetCreatureNode(player.Creature);
		if (creatureNode != null)
		{
			nGhostflames.Track(creatureNode, combatVfxContainer);
		}
		Displays[player] = nGhostflames;
		Refresh(player);
	}

	public static void Refresh(Player player)
	{
		NGhostflames visuals = GetVisuals(player);
		if (visuals == null)
		{
			if (!(player.Character is Hexaghost))
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			Setup(instance, player);
			visuals = GetVisuals(player);
			if (visuals == null)
			{
				return;
			}
		}
		GhostflameModel[] wheel = HexaghostCmd.GetWheel(player);
		int currentIndex = HexaghostCmd.GetCurrentIndex(player);
		visuals.RefreshWheel(wheel, currentIndex, player);
	}

	public static void RefreshCurrentIntent(Player player)
	{
		NGhostflames visuals = GetVisuals(player);
		if (visuals != null)
		{
			GhostflameModel[] wheel = HexaghostCmd.GetWheel(player);
			int currentIndex = HexaghostCmd.GetCurrentIndex(player);
			visuals.RefreshCurrentIntent(wheel, currentIndex, player);
		}
	}
}
