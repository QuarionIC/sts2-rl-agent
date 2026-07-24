using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace BaseLib.Abstracts;

public interface ICustomMessage : IPacketSerializable
{
	bool ShouldBroadcast { get; }

	bool ShouldBuffer => true;

	NetTransferMode Mode => (NetTransferMode)2;

	LogLevel LogLevel => (LogLevel)0;

	void HandleMessage(ulong senderId);
}
