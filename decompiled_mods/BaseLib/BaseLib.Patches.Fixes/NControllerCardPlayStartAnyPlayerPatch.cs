using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(NControllerCardPlay), "Start")]
internal static class NControllerCardPlayStartAnyPlayerPatch
{
	[HarmonyPrefix]
	private static bool ControllerCardPlay(NControllerCardPlay __instance)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(((NCardPlay)__instance).Card))
		{
			return true;
		}
		CardModel card = ((NCardPlay)__instance).Card;
		NCard cardNode = ((NCardPlay)__instance).CardNode;
		if (card == null || cardNode == null)
		{
			return false;
		}
		NDebugAudioManager instance = NDebugAudioManager.Instance;
		if (instance != null)
		{
			instance.Play("card_select.mp3", 1f, (PitchVariance)0);
		}
		NHoverTipSet.Remove((Control)(object)((NCardPlay)__instance).Holder);
		UnplayableReason val = default(UnplayableReason);
		AbstractModel val2 = default(AbstractModel);
		if (!card.CanPlay(ref val, ref val2))
		{
			((NCardPlay)__instance).CannotPlayThisCardFtueCheck(card);
			((NCardPlay)__instance).CancelPlayCard();
			LocString playerDialogueLine = UnplayableReasonExtensions.GetPlayerDialogueLine(val, val2);
			if (playerDialogueLine != null)
			{
				NCombatRoom instance2 = NCombatRoom.Instance;
				if (instance2 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)NThoughtBubbleVfx.Create(playerDialogueLine.GetFormattedText(), card.Owner.Creature, (double?)1.0));
				}
			}
			return false;
		}
		((NCardPlay)__instance).TryShowEvokingOrbs();
		cardNode.CardHighlight.AnimFlash();
		((NCardPlay)__instance).CenterCard();
		TaskHelper.RunSafely(__instance.SingleCreatureTargeting((TargetType)5));
		return false;
	}
}
