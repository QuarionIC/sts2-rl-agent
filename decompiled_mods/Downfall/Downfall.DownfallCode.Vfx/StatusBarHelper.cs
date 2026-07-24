using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Downfall.DownfallCode.Vfx;

public static class StatusBarHelper
{
	private const string NodeName = "ExtraStatusBar";

	public static NStatusBar? Get(Player player)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		NCreature creatureNode = instance.GetCreatureNode(player.Creature);
		if (creatureNode == null)
		{
			return null;
		}
		return ((Node)creatureNode._stateDisplay).GetNodeOrNull<NStatusBar>(NodePath.op_Implicit("ExtraStatusBar"));
	}

	public static void SetStatus(Player player, int current, int max, Color? color)
	{
		Get(player)?.SetStatus(current, max, color);
	}
}
