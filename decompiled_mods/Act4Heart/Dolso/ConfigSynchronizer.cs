using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Dolso;

internal sealed class ConfigSynchronizer : IDisposable
{
	private struct ValidateConfigMessage : INetMessage, IPacketSerializable
	{
		public int version;

		public readonly bool ShouldBroadcast => false;

		public readonly NetTransferMode Mode => (NetTransferMode)1;

		public readonly LogLevel LogLevel => (LogLevel)0;

		public bool ShouldBuffer => false;

		public ValidateConfigMessage(int version)
		{
			this.version = version;
		}

		public void Deserialize(PacketReader reader)
		{
			version = reader.ReadInt(32);
		}

		public readonly void Serialize(PacketWriter writer)
		{
			writer.WriteInt(version, 32);
		}
	}

	private struct ConfigMessage : INetMessage, IPacketSerializable
	{
		public Config config;

		public int version;

		public readonly bool ShouldBroadcast => false;

		public readonly NetTransferMode Mode => (NetTransferMode)2;

		public readonly LogLevel LogLevel => (LogLevel)0;

		public bool ShouldBuffer => false;

		public ConfigMessage(Config config, int version)
		{
			this.config = config;
			this.version = version;
		}

		public void Deserialize(PacketReader reader)
		{
			config = reader.Read<Config>();
			version = reader.ReadInt(32);
		}

		public readonly void Serialize(PacketWriter writer)
		{
			writer.Write<Config>(config);
			writer.WriteInt(version, 32);
		}
	}

	internal static ConfigSynchronizer? instance;

	internal static ConfigReader config_reader = null;

	private static readonly JsonSerializerOptions json_log = new JsonSerializerOptions(Config.JsonSerializer.Default.Options)
	{
		WriteIndented = false
	};

	private Config? config;

	private readonly INetGameService net_service;

	private int version;

	internal static Config current_config
	{
		get
		{
			ConfigSynchronizer configSynchronizer = instance;
			if (configSynchronizer == null || (object)configSynchronizer.config == null)
			{
				return config_reader.local_config;
			}
			return instance.config;
		}
	}

	private Config _active_config => config ?? config_reader.local_config;

	internal static event Action? on_dispose;

	private ConfigSynchronizer(INetGameService net_service)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		this.net_service = net_service;
		net_service.RegisterMessageHandler<ConfigMessage>((MessageHandlerDelegate<ConfigMessage>)HandleConfigMessageClient);
		net_service.RegisterMessageHandler<ValidateConfigMessage>((MessageHandlerDelegate<ValidateConfigMessage>)HandleValidateConfig);
		if ((int)net_service.Type != 3)
		{
			RunManager.Instance.RunStarted += OnRunStarted;
			RunManager.Instance.RoomEntered += OnRoomEntered;
			INetGameService obj = ((net_service is INetHostGameService) ? net_service : null);
			if (obj != null)
			{
				((INetHostGameService)obj).ClientConnected += OnClientConnectedServer;
			}
			UpdateConfigServer();
		}
	}

	internal static int Startup()
	{
		config_reader = ConfigReader.Startup(notify_combat_of_change: true, delegate
		{
			instance?.UpdateConfigServer();
		});
		return HookAttribute.ScanAndApply(typeof(ConfigSynchronizer));
	}

	public void Dispose()
	{
		net_service.UnregisterMessageHandler<ConfigMessage>((MessageHandlerDelegate<ConfigMessage>)HandleConfigMessageClient);
		RunManager.Instance.RunStarted -= OnRunStarted;
		RunManager.Instance.RoomEntered -= OnRoomEntered;
		INetGameService obj = net_service;
		INetGameService obj2 = ((obj is INetHostGameService) ? obj : null);
		if (obj2 != null)
		{
			((INetHostGameService)obj2).ClientConnected -= OnClientConnectedServer;
		}
	}

	public void UpdateConfigServer()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)net_service.Type != 3)
		{
			config = config_reader.local_config;
			version++;
			config_reader.OnConfigChanged();
			net_service.SendMessage<ConfigMessage>(new ConfigMessage(config, version));
		}
	}

	private void HandleConfigMessageClient(ConfigMessage message, ulong senderid)
	{
		if (IsClient(senderid, logerror: true, "HandleConfigMessageClient"))
		{
			bool num = version != message.version;
			config = message.config;
			version = message.version;
			config_reader.OnConfigChanged();
			if (num)
			{
				log.info("Received new config: " + JsonSerializer.Serialize(config, json_log));
			}
		}
	}

	private void OnRunStarted(RunState _)
	{
		UpdateConfigServer();
	}

	private void OnRoomEntered()
	{
		net_service.SendMessage<ValidateConfigMessage>(new ValidateConfigMessage(version));
	}

	private void OnClientConnectedServer(ulong playerid)
	{
		UpdateConfigServer();
	}

	private void HandleValidateConfig(ValidateConfigMessage message, ulong senderid)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)net_service.Type == 3)
		{
			if (message.version != version)
			{
				net_service.SendMessage<ValidateConfigMessage>(new ValidateConfigMessage(version));
			}
		}
		else if (message.version != version)
		{
			log.warning("Received a config request from sender " + senderid);
			net_service.SendMessage<ConfigMessage>(new ConfigMessage(_active_config, version), senderid);
		}
	}

	private bool IsClient(ulong senderid, bool logerror, [CallerMemberName] string caller = "")
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)net_service.Type != 3)
		{
			if (logerror)
			{
				log.error(caller + " received a client-only message as host from sender " + senderid);
			}
			return false;
		}
		return true;
	}

	[Hook(typeof(RunManager), "InitializeShared")]
	private static void InitSynchronizer_IL_RunManager_InitializeShared(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		val.Emit(OpCodes.Ldarg, ((IEnumerable<ParameterDefinition>)((MethodReference)il.Method).Parameters).First((ParameterDefinition a) => ((ParameterReference)a).Name == "netService"));
		val.EmitDelegate<Action<INetGameService>>((Action<INetGameService>)func);
		static void func(INetGameService net)
		{
			instance = new ConfigSynchronizer(net);
		}
	}

	[Hook(typeof(RunManager), "CleanUp")]
	private static void Dispose_IL_RunManager_CleanUp(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		new ILCursor(il).EmitDelegate<Action>((Action)func);
		static void func()
		{
			if (instance != null)
			{
				instance.Dispose();
				instance = null;
				ConfigSynchronizer.on_dispose?.Invoke();
			}
		}
	}
}
