using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
internal class TryPlayCardPatch
{
	[HarmonyPrefix]
	private static bool StopPlayIfCustomTargetInvalid(NCardPlay __instance, Creature? target)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		CardModel card = __instance.Card;
		if (card == null || !CustomTargetType.SingleTargeting.ContainsKey(card.TargetType))
		{
			return true;
		}
		if (target == null || ((NCardHolder)__instance.Holder).CardModel == null)
		{
			__instance.CancelPlayCard();
			return false;
		}
		if (!((NCardHolder)__instance.Holder).CardModel.CanPlayTargeting(target))
		{
			__instance.CannotPlayThisCardFtueCheck(((NCardHolder)__instance.Holder).CardModel);
			__instance.CancelPlayCard();
			return false;
		}
		__instance._isTryingToPlayCard = true;
		bool num = card.TryManualPlay(target);
		__instance._isTryingToPlayCard = false;
		if (num)
		{
			__instance.AutoDisableCannotPlayCardFtueCheck();
			if (((Node)__instance.Holder).IsInsideTree())
			{
				Rect2 visibleRect = ((Node)__instance).GetViewport().GetVisibleRect();
				Vector2 size = ((Rect2)(ref visibleRect)).Size;
				__instance.Holder.SetTargetPosition(new Vector2(size.X / 2f, size.Y - ((Control)__instance.Holder).Size.Y));
			}
			AccessTools.Method(typeof(NCardPlay), "Cleanup", (Type[])null, (Type[])null).Invoke(__instance, new object[1] { true });
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return false;
			}
			NodeUtil.TryGrabFocus((Control)(object)instance.Ui.Hand);
		}
		else
		{
			__instance.CancelPlayCard();
		}
		return false;
	}
}
