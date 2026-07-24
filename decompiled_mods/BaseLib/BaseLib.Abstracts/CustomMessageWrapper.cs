using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public sealed class CustomMessageWrapper : INetMessage, IPacketSerializable
{
	public required ICustomMessage Message;

	private static List<Type>? _customMessages;

	private static readonly Dictionary<Type, int> CustomMessageToId = new Dictionary<Type, int>();

	private static readonly Dictionary<int, Type> IdToCustomMessage = new Dictionary<int, Type>();

	public static byte WrapperMessageId { get; set; }

	public static List<Type> CustomMessages
	{
		get
		{
			if (_customMessages == null)
			{
				_customMessages = ReflectionHelper.GetSubtypesInMods<ICustomMessage>().ToList();
				_customMessages.Sort((Type a, Type b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
			}
			return _customMessages;
		}
	}

	public int MessageType => CustomMessageToId[Message.GetType()];

	public bool ShouldBroadcast => Message.ShouldBroadcast;

	public bool ShouldBuffer => Message.ShouldBuffer;

	public NetTransferMode Mode => Message.Mode;

	public LogLevel LogLevel => Message.LogLevel;

	public static void Initialize()
	{
		foreach (Type customMessage in CustomMessages)
		{
			int i;
			Type value;
			for (i = (customMessage.FullName ?? customMessage.Name).ComputeBasicHash(); IdToCustomMessage.TryGetValue(i, out value); i++)
			{
				BaseLibMain.Logger.Warn($"Message key hash collision: {value} and {customMessage} with key {i}", 1);
			}
			IdToCustomMessage[i] = customMessage;
			CustomMessageToId[customMessage] = i;
		}
	}

	internal static void Register(INetGameService messageBuffer)
	{
		messageBuffer.RegisterMessageHandler<CustomMessageWrapper>((MessageHandlerDelegate<CustomMessageWrapper>)HandleCustomMessage);
	}

	internal static void Unregister(INetGameService messageBuffer)
	{
		messageBuffer.UnregisterMessageHandler<CustomMessageWrapper>((MessageHandlerDelegate<CustomMessageWrapper>)HandleCustomMessage);
	}

	private static void HandleCustomMessage(CustomMessageWrapper message, ulong senderId)
	{
		message.Message.HandleMessage(senderId);
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
		Message = (ICustomMessage)Activator.CreateInstance(type);
		((IPacketSerializable)Message).Deserialize(reader);
	}

	public static void Send(ICustomMessage msg, INetGameService? netService = null)
	{
		(netService ?? RunManager.Instance.NetService).SendMessage<CustomMessageWrapper>(new CustomMessageWrapper
		{
			Message = msg
		});
	}
}
