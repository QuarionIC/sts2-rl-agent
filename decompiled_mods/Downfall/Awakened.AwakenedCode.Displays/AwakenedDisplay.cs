using System.Collections.Generic;
using Awakened.AwakenedCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Awakened.AwakenedCode.Displays;

public class AwakenedDisplay
{
	private static readonly Dictionary<Player, NSpellbookDisplay> Displays = new Dictionary<Player, NSpellbookDisplay>();

	public static bool HasDisplay(Player player)
	{
		if (Displays.TryGetValue(player, out NSpellbookDisplay value))
		{
			return GodotObject.IsInstanceValid((GodotObject)(object)value);
		}
		return false;
	}

	public static void Register(Player player, NSpellbookDisplay display)
	{
		if (Displays.TryGetValue(player, out NSpellbookDisplay value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((Node)value).QueueFree();
		}
		Displays[player] = display;
	}

	public static void Refresh(Player player)
	{
		Displays.GetValueOrDefault(player)?.Refresh();
	}
}
