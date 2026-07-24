using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting")]
internal static class NControllerCardPlaySingleTargetingAnyPlayerPatch
{
	[HarmonyPrefix]
	private static bool ControllerTargeting(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		if ((int)targetType != 5)
		{
			return true;
		}
		__result = AnyPlayerControllerTargeting(__instance);
		return false;
	}

	private static async Task AnyPlayerControllerTargeting(NControllerCardPlay instance)
	{
		CardModel card = ((NCardPlay)instance).Card;
		if (card == null)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		ICombatState val = card.CombatState ?? card.Owner.Creature.CombatState;
		if (val == null)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		NCard cardNode = ((NCardPlay)instance).CardNode;
		if (cardNode == null)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		NTargetManager targetManager = NTargetManager.Instance;
		List<Creature> list = val.PlayerCreatures.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer).ToList();
		if (list.Count == 0)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		List<NCreature> list2 = list.Select((Creature c) => NCombatRoom.Instance.GetCreatureNode(c)).OfType<NCreature>().ToList();
		if (list2.Count == 0)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		Callable hoverCallable = Callable.From<NCreature>((Action<NCreature>)delegate(NCreature c)
		{
			((NCardPlay)instance).OnCreatureHover(c);
		});
		Callable unhoverCallable = Callable.From<NCreature>((Action<NCreature>)delegate(NCreature c)
		{
			((NCardPlay)instance).OnCreatureUnhover(c);
		});
		try
		{
			((GodotObject)targetManager).Connect(SignalName.CreatureHovered, hoverCallable, 0u);
			((GodotObject)targetManager).Connect(SignalName.CreatureUnhovered, unhoverCallable, 0u);
			targetManager.StartTargeting((TargetType)5, (Control)(object)cardNode, (TargetMode)3, (Func<bool>)(() => !GodotObject.IsInstanceValid((GodotObject)(object)instance) || !NControllerManager.Instance.IsUsingController), (Func<Node, bool>)null);
			NCombatRoom.Instance.RestrictControllerNavigation(list2.Select((NCreature n) => n.Hitbox));
			NodeUtil.TryGrabFocus(list2.First().Hitbox);
			NCreature val2 = (NCreature)(await targetManager.SelectionFinished());
			if (GodotObject.IsInstanceValid((GodotObject)(object)instance))
			{
				if (val2 != null)
				{
					((NCardPlay)instance).TryPlayCard(val2.Entity);
				}
				else
				{
					((NCardPlay)instance).CancelPlayCard();
				}
			}
		}
		finally
		{
			if (((GodotObject)targetManager).IsConnected(SignalName.CreatureHovered, hoverCallable))
			{
				((GodotObject)targetManager).Disconnect(SignalName.CreatureHovered, hoverCallable);
			}
			if (((GodotObject)targetManager).IsConnected(SignalName.CreatureUnhovered, unhoverCallable))
			{
				((GodotObject)targetManager).Disconnect(SignalName.CreatureUnhovered, unhoverCallable);
			}
		}
	}
}
