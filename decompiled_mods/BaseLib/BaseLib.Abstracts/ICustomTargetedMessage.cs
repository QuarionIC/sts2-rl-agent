using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public interface ICustomTargetedMessage : IPacketSerializable
{
	bool IsRewardMessage { get; }

	RunLocation Location { get; }

	bool ShouldBroadcast { get; }

	bool ShouldBuffer => true;

	NetTransferMode Mode => (NetTransferMode)2;

	LogLevel LogLevel => (LogLevel)0;

	void HandleMessage(ulong senderId);
}
