using System;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace BaseLib.Patches.Networking;

[HarmonyPatch(typeof(MessageTypes), "TryGetMessageType")]
internal static class GetCustomTypes
{
	[HarmonyPrefix]
	private static bool CustomWrapperTypes(int id, ref Type? type, ref bool __result)
	{
		if (id == CustomMessageWrapper.WrapperMessageId)
		{
			type = typeof(CustomMessageWrapper);
			__result = true;
			return false;
		}
		if (id == CustomTargetedMessageWrapper.WrapperMessageId)
		{
			type = typeof(CustomTargetedMessageWrapper);
			__result = true;
			return false;
		}
		return true;
	}
}
