using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast;

public static class GremlinLeaderHelper
{
	private static readonly LocString _fleeLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee1");

	private static readonly LocString _fleeLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee2");

	public static void SubscribeToLeaderDeath(Creature gremlin, CombatState combatState)
	{
		Creature leader = ((IEnumerable<Creature>)combatState.GetTeammatesOf(gremlin)).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.Monster is GremlinLeader));
		if (leader != null)
		{
			leader.Died += OnLeaderDied;
		}
		async void OnLeaderDied(Creature _)
		{
			leader.Died -= OnLeaderDied;
			if (!gremlin.IsDead)
			{
				List<Creature> livingGremlins = (from t in combatState.GetTeammatesOf(gremlin)
					where t != leader && t.IsAlive
					select t).ToList();
				LocString line = ((livingGremlins.FirstOrDefault() == gremlin) ? _fleeLine1 : _fleeLine2);
				TalkCmd.Play(line, gremlin, (VfxColor)5, (VfxDuration)2);
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(gremlin) : null);
				if (creatureNode != null)
				{
					creatureNode.ToggleIsInteractable(false);
				}
				await CreatureCmd.Escape(gremlin, false);
				((List<Creature>)combatState.EscapedCreatures).Remove(gremlin);
				await EscapeAnimation.Play(gremlin);
				if (creatureNode != null)
				{
					((CanvasItem)creatureNode).Visible = false;
					NCombatRoom instance2 = NCombatRoom.Instance;
					if (instance2 != null)
					{
						instance2.RemoveCreatureNode(creatureNode);
					}
				}
			}
		}
	}
}
