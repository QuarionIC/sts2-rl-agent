using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting", new Type[] { typeof(TargetType) })]
internal class ControllerSingleCreatureTargetingPatch
{
	[HarmonyPrefix]
	private static bool CustomControllerTargeting(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!CustomTargetType.SingleTargeting.TryGetValue(targetType, out Func<Creature, Player, bool> value))
		{
			return true;
		}
		__result = FilteredControllerTargeting(__instance, targetType, value);
		return false;
	}

	private static async Task FilteredControllerTargeting(NControllerCardPlay instance, TargetType targetType, Func<Creature, Player, bool> filter)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		CardModel card = ((NCardPlay)instance).Card;
		NCard cardNode = ((NCardPlay)instance).CardNode;
		CardModel obj = card;
		if (((obj != null) ? obj.CombatState : null) == null || cardNode == null)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		if (instance2 == null)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		List<NCreature> list = instance2.CreatureNodes.Where((NCreature n) => filter(n.Entity, card.Owner)).ToList();
		if (list.Count == 0)
		{
			((NCardPlay)instance).CancelPlayCard();
			return;
		}
		NTargetManager targetManager = NTargetManager.Instance;
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
			targetManager.StartTargeting(targetType, (Control)(object)cardNode, (TargetMode)3, (Func<bool>)(() => !GodotObject.IsInstanceValid((GodotObject)(object)instance) || !NControllerManager.Instance.IsUsingController), (Func<Node, bool>)null);
			instance2.RestrictControllerNavigation(list.Select((NCreature n) => n.Hitbox));
			NodeUtil.TryGrabFocus(list.First().Hitbox);
			NCreature val = (NCreature)(await targetManager.SelectionFinished());
			if (GodotObject.IsInstanceValid((GodotObject)(object)instance))
			{
				if (val != null)
				{
					((NCardPlay)instance).TryPlayCard(val.Entity);
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
