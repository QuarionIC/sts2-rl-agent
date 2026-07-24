using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public sealed class CustomTargetedMessageWrapper : IRunLocationTargetedMessage, INetMessage, IPacketSerializable
{
	public required ICustomTargetedMessage Message;

	private static List<Type>? _targetMessages;

	private static readonly Dictionary<Type, int> CustomMessageToId = new Dictionary<Type, int>();

	private static readonly Dictionary<int, Type> IdToCustomMessage = new Dictionary<int, Type>();

	public static byte WrapperMessageId { get; set; }

	public static List<Type> TargetedMessages
	{
		get
		{
			if (_targetMessages == null)
			{
				_targetMessages = ReflectionHelper.GetSubtypesInMods<ICustomTargetedMessage>().ToList();
				_targetMessages.Sort((Type a, Type b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
			}
			return _targetMessages;
		}
	}

	public int MessageType => CustomMessageToId[Message.GetType()];

	public RunLocation Location => Message.Location;

	public bool ShouldBroadcast => Message.ShouldBroadcast;

	public bool ShouldBuffer => Message.ShouldBuffer;

	public NetTransferMode Mode => Message.Mode;

	public LogLevel LogLevel => Message.LogLevel;

	public static void Initialize()
	{
		foreach (Type targetedMessage in TargetedMessages)
		{
			int i;
			Type value;
			for (i = (targetedMessage.FullName ?? targetedMessage.Name).ComputeBasicHash(); IdToCustomMessage.TryGetValue(i, out value); i++)
			{
				BaseLibMain.Logger.Warn($"Message key hash collision: {value} and {targetedMessage} with key {i}", 1);
			}
			IdToCustomMessage[i] = targetedMessage;
			CustomMessageToId[targetedMessage] = i;
		}
	}

	internal static void Register(RunLocationTargetedMessageBuffer messageBuffer)
	{
		messageBuffer.RegisterMessageHandler<CustomTargetedMessageWrapper>((MessageHandlerDelegate<CustomTargetedMessageWrapper>)HandleCustomMessage);
	}

	internal static void Unregister(RunLocationTargetedMessageBuffer messageBuffer)
	{
		messageBuffer.UnregisterMessageHandler<CustomTargetedMessageWrapper>((MessageHandlerDelegate<CustomTargetedMessageWrapper>)HandleCustomMessage);
	}

	private static void HandleCustomMessage(CustomTargetedMessageWrapper message, ulong senderId)
	{
		RewardSynchronizer rewardSynchronizer = RunManager.Instance.RewardSynchronizer;
		if (CombatManager.Instance.IsInProgress && message.Message.IsRewardMessage)
		{
			rewardSynchronizer.BufferCustomRewardMessage(message, senderId);
			BaseLibMain.Logger.Debug($"Buffered {message.Message.GetType()} message", 1);
		}
		else
		{
			message.Message.HandleMessage(senderId);
		}
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteInt(MessageType, 32);
		((IPacketSerializable)Message).Serialize(writer);
	}

	public void Deserialize(PacketReader reader)
	{
		int key = reader.ReadInt(32);
		Type type = IdToCustomMessage[key];
		Message = (ICustomTargetedMessage)Activator.CreateInstance(type);
		((IPacketSerializable)Message).Deserialize(reader);
	}

	public static void Send(ICustomTargetedMessage msg, INetGameService? netService = null)
	{
		(netService ?? RunManager.Instance.NetService).SendMessage<CustomTargetedMessageWrapper>(new CustomTargetedMessageWrapper
		{
			Message = msg
		});
	}
}
