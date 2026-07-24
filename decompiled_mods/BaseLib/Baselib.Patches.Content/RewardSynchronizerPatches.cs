using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(RewardSynchronizer))]
public static class RewardSynchronizerPatches
{
	public struct BufferedCustomRewardMessage
	{
		public ulong SenderId;

		public CustomTargetedMessageWrapper Message;
	}

	[SpecialName]
	public sealed class _003CG_003E_002417FE65D7FCD1FE422A3D4E29C1FE8F01
	{
		[SpecialName]
		public static class _003CM_003E_0024A0C79FC3C0AC9B189888D2E608DEB92A
		{
		}

		[ExtensionMarker("<M>$A0C79FC3C0AC9B189888D2E608DEB92A")]
		public INetGameService GameService()
		{
			throw null;
		}

		[ExtensionMarker("<M>$A0C79FC3C0AC9B189888D2E608DEB92A")]
		public void BufferCustomRewardMessage(CustomTargetedMessageWrapper message, ulong senderId)
		{
			throw null;
		}
	}

	internal static readonly SpireField<RewardSynchronizer, List<BufferedCustomRewardMessage>> BufferedCustomRewardMessages = new SpireField<RewardSynchronizer, List<BufferedCustomRewardMessage>>(() => new List<BufferedCustomRewardMessage>());

	public static INetGameService GameService(this RewardSynchronizer rewardSynchronizer)
	{
		return rewardSynchronizer._gameService;
	}

	public static void BufferCustomRewardMessage(this RewardSynchronizer rewardSynchronizer, CustomTargetedMessageWrapper message, ulong senderId)
	{
		BufferedCustomRewardMessage item = new BufferedCustomRewardMessage
		{
			SenderId = senderId,
			Message = message
		};
		BufferedCustomRewardMessages[rewardSynchronizer]?.Add(item);
	}

	[HarmonyPatch("OnCombatEnded")]
	[HarmonyPrefix]
	private static void OnCombatEndHandleCustomBufferedMessages(RewardSynchronizer __instance)
	{
		foreach (BufferedCustomRewardMessage item in BufferedCustomRewardMessages[__instance])
		{
			__instance._messageBuffer.CallHandlersOfType(item.Message.GetType(), (INetMessage)(object)item.Message, item.SenderId);
		}
		BufferedCustomRewardMessages[__instance]?.Clear();
	}
}
