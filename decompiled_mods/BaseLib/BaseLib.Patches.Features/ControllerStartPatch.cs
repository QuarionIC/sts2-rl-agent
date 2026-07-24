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

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NControllerCardPlay), "Start")]
internal class ControllerStartPatch
{
	[HarmonyPrefix]
	private static bool CustomControllerPlayStart(NControllerCardPlay __instance)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		CardModel card = ((NCardPlay)__instance).Card;
		NCard cardNode = ((NCardPlay)__instance).CardNode;
		if (card == null || cardNode == null || !CustomTargetType.SingleTargeting.ContainsKey(card.TargetType))
		{
			return true;
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
		TaskHelper.RunSafely(__instance.SingleCreatureTargeting(card.TargetType));
		return false;
	}
}
