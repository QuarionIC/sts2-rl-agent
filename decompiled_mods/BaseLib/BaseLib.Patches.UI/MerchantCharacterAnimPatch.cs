using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal class MerchantCharacterAnimPatch
{
	[HarmonyPatch(typeof(NMerchantCharacter), "_Ready")]
	[HarmonyPrefix]
	public static bool SkipInitialAnimIfNotSpine(NMerchantCharacter __instance)
	{
		if (!NodeFactory.CreatedFromFactory((Node)(object)__instance))
		{
			return true;
		}
		if (CustomAnimation.HasCustomAnimation((Node)(object)__instance))
		{
			return false;
		}
		if (((Node)__instance).GetChildCount(false) == 0)
		{
			return false;
		}
		GodotObject child = (GodotObject)(object)((Node)__instance).GetChild(0, false);
		if (child == null || child.GetClass() != "SpineSprite")
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(NMerchantCharacter), "PlayAnimation")]
	[HarmonyPrefix]
	public static bool PlayAlternateAnimation(NMerchantCharacter __instance, string anim, bool loop)
	{
		if (!NodeFactory.CreatedFromFactory((Node)(object)__instance))
		{
			return true;
		}
		if (CustomAnimation.PlayCustomAnimation((Node)(object)__instance, GetAnimNames(anim)))
		{
			return false;
		}
		if (((Node)__instance).GetChildCount(false) == 0)
		{
			return false;
		}
		GodotObject child = (GodotObject)(object)((Node)__instance).GetChild(0, false);
		if (child == null || child.GetClass() != "SpineSprite")
		{
			return false;
		}
		return true;
	}

	private static string[] GetAnimNames(string animName)
	{
		if (!(animName == "relaxed_loop"))
		{
			if (animName == "die")
			{
				return new string[2] { "Die", animName };
			}
			return new string[1] { animName };
		}
		return new string[3] { "idle", "Idle", animName };
	}
}
