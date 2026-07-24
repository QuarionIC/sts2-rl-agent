using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Guardian.GuardianCode.Rewards;

public class CardsAddedMessage : ICustomMessage, IPacketSerializable
{
	public bool WasSkipped { get; set; }

	public List<SerializableCard> Cards { get; init; } = new List<SerializableCard>();

	public bool ShouldBroadcast => false;

	public NetTransferMode Mode => (NetTransferMode)2;

	public LogLevel LogLevel => (LogLevel)2;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteBool(WasSkipped);
		writer.WriteInt(Cards.Count, 32);
		foreach (SerializableCard card in Cards)
		{
			card.Serialize(writer);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		WasSkipped = reader.ReadBool();
		int num = reader.ReadInt(32);
		for (int i = 0; i < num; i++)
		{
			SerializableCard val = new SerializableCard();
			val.Deserialize(reader);
			Cards.Add(val);
		}
	}

	public void HandleMessage(ulong senderId)
	{
		if (WasSkipped || Cards.Count == 0)
		{
			return;
		}
		RunState state = RunManager.Instance.State;
		Player player = ((state != null) ? state.GetPlayer(senderId) : null);
		if (player == null)
		{
			return;
		}
		CardPileCmd.Add(((IEnumerable<SerializableCard>)Cards).Select((Func<SerializableCard, CardModel>)CardModel.FromSerializable), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		if (LocalContext.IsMe(player))
		{
			return;
		}
		NRun instance = NRun.Instance;
		NMultiplayerPlayerStateContainer obj = ((instance != null) ? instance.GlobalUi.MultiplayerPlayerContainer : null);
		NMultiplayerPlayerState val = ((obj != null) ? ((IEnumerable)((Node)obj).GetChildren(false)).OfType<NMultiplayerPlayerState>().FirstOrDefault((Func<NMultiplayerPlayerState, bool>)((NMultiplayerPlayerState s) => s.Player == player)) : null);
		if (val == null)
		{
			return;
		}
		foreach (CardModel item in ((IEnumerable<SerializableCard>)Cards).Select((Func<SerializableCard, CardModel>)CardModel.FromSerializable))
		{
			TaskHelper.RunSafely(val.AnimateCardObtained(item));
		}
	}
}
