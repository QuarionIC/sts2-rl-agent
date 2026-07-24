using System;
using System.Reflection;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace BaseLib.Patches.Networking;

[HarmonyPatch]
internal static class AdjustCustomMessageKeys
{
	private static MethodBase? _target;

	private static bool Prepare()
	{
		if ((object)_target == null)
		{
			_target = (MethodBase?)(((object)AccessTools.DeclaredMethod(typeof(MessageTypes), "Initialize", (Type[])null, (Type[])null)) ?? ((object)typeof(MessageTypes).TypeInitializer));
		}
		if ((object)_target == null)
		{
			BaseLibMain.Logger.Warn("MessageTypes.Initialize (and its static constructor) could not be found; custom network message wrappers will not be registered for this game version.", 1);
		}
		return (object)_target != null;
	}

	private static MethodBase TargetMethod()
	{
		return _target;
	}

	[HarmonyPostfix]
	private static void Fuckery()
	{
		BaseLibMain.Logger.Info("Adjusting keys of custom message wrappers.", 1);
		NetTypeCache<INetMessage> cache = MessageTypes._cache;
		cache._idToType.Remove(typeof(CustomMessageWrapper));
		cache._idToType.Remove(typeof(CustomTargetedMessageWrapper));
		for (int i = 0; i < cache._idToType.Count; i++)
		{
			Type key = cache._idToType[i];
			cache._typeToId[key] = i;
		}
		byte b = 128;
		Type type = default(Type);
		while (b < byte.MaxValue && cache.TryGetTypeFromId((int)b, ref type))
		{
			b++;
		}
		CustomMessageWrapper.WrapperMessageId = b;
		cache._typeToId[typeof(CustomMessageWrapper)] = b;
		b++;
		while (b < byte.MaxValue && cache.TryGetTypeFromId((int)b, ref type))
		{
			b++;
		}
		CustomTargetedMessageWrapper.WrapperMessageId = b;
		cache._typeToId[typeof(CustomTargetedMessageWrapper)] = b;
		BaseLibMain.Logger.Info($"Using IDs {CustomMessageWrapper.WrapperMessageId} and {CustomTargetedMessageWrapper.WrapperMessageId} for custom message wrappers.", 1);
	}
}
