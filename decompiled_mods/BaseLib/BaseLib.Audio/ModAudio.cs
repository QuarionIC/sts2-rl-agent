using System;
using System.Collections.Generic;
using BaseLib.Config;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Audio;

public static class ModAudio
{
	private class StreamPlayerPool
	{
		public Queue<AudioStreamPlayer> Players = new Queue<AudioStreamPlayer>();

		public int MaxCount;
	}

	public enum SoundType
	{
		Sfx,
		Music,
		Ambience
	}

	public static readonly SpireField<AudioStreamPlayer, Func<float, float>> VolumeModDb;

	internal static readonly StringName SfxBus;

	internal static readonly StringName MasterBus;

	private static Dictionary<SoundType, StreamPlayerPool> _playerPools;

	private static List<AudioStreamPlayer> _activeMusic;

	private static List<AudioStreamPlayer> _activeAmbience;

	private static float MasterVol => SaveManager.Instance.SettingsSave.VolumeMaster;

	static ModAudio()
	{
		VolumeModDb = new SpireField<AudioStreamPlayer, Func<float, float>>(() => (float val) => val);
		SfxBus = StringName.op_Implicit("SFX");
		MasterBus = StringName.op_Implicit("Master");
		_playerPools = new Dictionary<SoundType, StreamPlayerPool>();
		_activeMusic = new List<AudioStreamPlayer>();
		_activeAmbience = new List<AudioStreamPlayer>();
		_playerPools[SoundType.Sfx] = new StreamPlayerPool();
		_playerPools[SoundType.Music] = new StreamPlayerPool();
		_playerPools[SoundType.Ambience] = new StreamPlayerPool();
	}

	private static int LimitForSoundType(SoundType soundType)
	{
		return soundType switch
		{
			SoundType.Sfx => BaseLibConfig.SfxPlayerLimit, 
			SoundType.Music => 2, 
			SoundType.Ambience => 2, 
			_ => 0, 
		};
	}

	private static float VolumeForSound(SoundType soundType)
	{
		return soundType switch
		{
			SoundType.Music => SaveManager.Instance.SettingsSave.VolumeBgm, 
			SoundType.Ambience => SaveManager.Instance.SettingsSave.VolumeAmbience, 
			_ => 1f, 
		};
	}

	private static StringName BusForSound(SoundType soundType)
	{
		if (soundType == SoundType.Sfx)
		{
			return SfxBus;
		}
		return MasterBus;
	}

	public static void UpdateVolumes()
	{
		float arg = VolumeForSound(SoundType.Music);
		float arg2 = VolumeForSound(SoundType.Ambience);
		foreach (AudioStreamPlayer item in _activeAmbience)
		{
			Tween? obj = AudioStreamPlayerExtensions.CurrentTween[item];
			if (obj != null)
			{
				obj.Kill();
			}
			item.VolumeDb = VolumeModDb[item](arg2);
		}
		foreach (AudioStreamPlayer item2 in _activeMusic)
		{
			Tween? obj2 = AudioStreamPlayerExtensions.CurrentTween[item2];
			if (obj2 != null)
			{
				obj2.Kill();
			}
			item2.VolumeDb = VolumeModDb[item2](arg);
		}
	}

	internal static AudioStreamPlayer? GetPlayerForSound(SoundType soundType)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		AudioStreamPlayer player;
		while (true)
		{
			if (!_playerPools.TryGetValue(soundType, out StreamPlayerPool players))
			{
				throw new ArgumentException($"Sound type '{soundType}' not found");
			}
			if (!players.Players.TryDequeue(out player))
			{
				if (players.MaxCount >= LimitForSoundType(soundType))
				{
					BaseLibMain.Logger.Warn($"Too many sounds for sound type '{soundType}'!", 1);
					return null;
				}
				BaseLibMain.Logger.Info($"Creating new player for {soundType} (Count: {players.MaxCount + 1})", 1);
				player = new AudioStreamPlayer
				{
					Bus = BusForSound(soundType)
				};
				((Node)player).TreeEntered += delegate
				{
					player.Play(0f);
				};
				player.Finished += delegate
				{
					Node parent = ((Node)player).GetParent();
					if (parent != null)
					{
						GodotTreeExtensions.RemoveChildSafely(parent, (Node)(object)player);
					}
				};
				((Node)player).TreeExited += delegate
				{
					player.Stream = null;
					players.Players.Enqueue(player);
				};
				switch (soundType)
				{
				case SoundType.Music:
					((Node)player).TreeEntered += delegate
					{
						_activeMusic.Add(player);
					};
					((Node)player).TreeExited += delegate
					{
						_activeMusic.Remove(player);
					};
					break;
				case SoundType.Ambience:
					((Node)player).TreeEntered += delegate
					{
						_activeAmbience.Add(player);
					};
					((Node)player).TreeExited += delegate
					{
						_activeAmbience.Remove(player);
					};
					break;
				}
				players.MaxCount++;
				break;
			}
			if (NodeUtil.IsValid((Node)(object)player))
			{
				break;
			}
			players.MaxCount--;
		}
		return player;
	}

	public static AudioStreamPlayer? PlaySoundGlobal(ModSound sound, float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		return PlaySound(sound, volumeAdd, volumeMult, pitchVariation, basePitch, (Node?)(object)((val != null) ? val.Root : null));
	}

	public static AudioStreamPlayer? PlaySoundInRun(ModSound sound, float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		return PlaySound(sound, volumeAdd, volumeMult, pitchVariation, basePitch, (Node?)(object)NRun.Instance);
	}

	public static AudioStreamPlayer? PlaySound(ModSound sound, float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f, Node? targetNode = null)
	{
		if (MasterVol <= 0f)
		{
			return null;
		}
		if (sound.SoundType switch
		{
			SoundType.Music => SaveManager.Instance.SettingsSave.VolumeBgm, 
			SoundType.Ambience => SaveManager.Instance.SettingsSave.VolumeAmbience, 
			_ => SaveManager.Instance.SettingsSave.VolumeSfx, 
		} < 0f)
		{
			return null;
		}
		if (sound.SoundType == SoundType.Music)
		{
			foreach (AudioStreamPlayer item in _activeMusic)
			{
				if (((Node)item).Name == StringName.op_Implicit(sound.File))
				{
					return null;
				}
				item.Stop();
				Node parent = ((Node)item).GetParent();
				if (parent != null)
				{
					GodotTreeExtensions.RemoveChildSafely(parent, (Node)(object)item);
				}
			}
		}
		else if (sound.SoundType == SoundType.Ambience)
		{
			foreach (AudioStreamPlayer item2 in _activeAmbience)
			{
				if (((Node)item2).Name == StringName.op_Implicit(sound.File))
				{
					return null;
				}
				item2.Stop();
				Node parent2 = ((Node)item2).GetParent();
				if (parent2 != null)
				{
					GodotTreeExtensions.RemoveChildSafely(parent2, (Node)(object)item2);
				}
			}
		}
		AudioStream orLoadStream = sound.GetOrLoadStream();
		if (orLoadStream == null)
		{
			BaseLibMain.Logger.Warn("Failed to get stream for sound " + sound.File, 1);
			return null;
		}
		AudioStreamPlayer playerForSound = GetPlayerForSound(sound.SoundType);
		if (playerForSound == null)
		{
			return null;
		}
		((Node)playerForSound).Name = StringName.op_Implicit(sound.File);
		playerForSound.Stream = orLoadStream;
		playerForSound.VolumeDb = Mathf.LinearToDb(VolumeForSound(sound.SoundType) * volumeMult) + volumeAdd * volumeMult;
		VolumeModDb[playerForSound] = (float val) => Mathf.LinearToDb(val * volumeMult) + volumeAdd * volumeMult;
		playerForSound.PitchScale = ((pitchVariation > 0f) ? (basePitch + (float)Rng.Chaotic.NextDouble() * 2f * pitchVariation - pitchVariation) : basePitch);
		if (targetNode == null && sound.SoundType == SoundType.Sfx)
		{
			targetNode = (Node?)(object)NCombatRoom.Instance;
		}
		if (targetNode == null)
		{
			targetNode = (Node?)(object)NRun.Instance;
		}
		if (targetNode == null)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			MainLoop obj = ((mainLoop is SceneTree) ? mainLoop : null);
			targetNode = (Node?)(object)((obj != null) ? ((SceneTree)obj).Root : null);
		}
		if (targetNode != null)
		{
			GodotTreeExtensions.AddChildSafely(targetNode, (Node)(object)playerForSound);
			return playerForSound;
		}
		BaseLibMain.Logger.Warn("Failed to play sound " + sound.File + "; unable to find node to attach player", 1);
		playerForSound.Stream = null;
		if (_playerPools.TryGetValue(sound.SoundType, out StreamPlayerPool value))
		{
			value.Players.Enqueue(playerForSound);
		}
		return null;
	}
}
