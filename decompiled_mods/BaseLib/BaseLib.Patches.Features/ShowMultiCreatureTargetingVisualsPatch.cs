using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")]
internal class ShowMultiCreatureTargetingVisualsPatch
{
	public static void Postfix(NCardPlay __instance)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (__instance.Card == null || !CustomTargetType.MultiTargeting.TryGetValue(__instance.Card.TargetType, out Func<Creature, Player, bool> value))
		{
			return;
		}
		NCard cardNode = __instance.CardNode;
		if (cardNode != null)
		{
			cardNode.UpdateVisuals(__instance.Card.Pile.Type, (CardPreviewMode)3);
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (value(creatureNode.Entity, __instance.Card.Owner))
			{
				creatureNode.ShowMultiselectReticle();
			}
		}
	}
}
