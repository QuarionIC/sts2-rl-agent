using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace ActsFromThePast;

public static class AFTPModAudio
{
	private static readonly Dictionary<string, AudioStream> CachedStreams = new Dictionary<string, AudioStream>();

	private static AudioStreamPlayer? _musicPlayer;

	private static string? _currentMusicPath;

	private static float _currentVolumeOffset = 0f;

	private static Tween? _fadeTween;

	private static AudioStreamPlayer? _outgoingPlayer;

	private static Tween? _outgoingFadeTween;

	private static AudioStreamPlayer? _ambiencePlayer;

	private static string? _currentAmbiencePath;

	private static Tween? _ambienceFadeTween;

	private const float MusicVolumeOffset = -3f;

	private const float AmbienceVolumeOffset = -6f;

	private const float SfxVolumeOffset = 0f;

	private static readonly string[] BossStingers = new string[4] { "boss_victory_stinger_1", "boss_victory_stinger_2", "boss_victory_stinger_3", "boss_victory_stinger_4" };

	public static void Play(string folder, string soundName, float volume = 0f, float pitchVariation = 0f, float basePitch = 1f)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		AudioStream orLoadStream = GetOrLoadStream(folder, soundName);
		if (orLoadStream == null)
		{
			return;
		}
		AudioStreamPlayer player = new AudioStreamPlayer();
		player.Stream = orLoadStream;
		player.VolumeDb = volume + 0f;
		player.Bus = StringName.op_Implicit("SFX");
		if (pitchVariation > 0f)
		{
			player.PitchScale = basePitch + (float)Rng.Chaotic.NextDouble() * 2f * pitchVariation - pitchVariation;
		}
		else
		{
			player.PitchScale = basePitch;
		}
		object obj = NRun.Instance;
		if (obj == null)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			MainLoop obj2 = ((mainLoop is SceneTree) ? mainLoop : null);
			obj = ((obj2 != null) ? ((SceneTree)obj2).Root : null);
		}
		Node val = (Node)obj;
		if (val != null)
		{
			val.AddChild((Node)(object)player, false, (InternalMode)0);
			player.Play(0f);
			player.Finished += delegate
			{
				((Node)player).QueueFree();
			};
		}
	}

	public static void Play(Creature creature, string folder, string soundName, float volume = 0f)
	{
		Play(folder, soundName, volume);
	}

	private static AudioStream? GetOrLoadStream(string folder, string soundName)
	{
		string key = folder + "/" + soundName;
		if (CachedStreams.TryGetValue(key, out AudioStream value))
		{
			return value;
		}
		string text = $"res://ActsFromThePast/sfx/{folder}/{soundName}.ogg";
		AudioStream val = GD.Load<AudioStream>(text);
		if (val != null)
		{
			CachedStreams[key] = val;
		}
		return val;
	}

	public static void PlayMusic(string[] musicOptions, float volumeDbOffset = 0f)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		if (musicOptions == null || musicOptions.Length == 0)
		{
			return;
		}
		string text = musicOptions[GD.RandRange(0, musicOptions.Length - 1)];
		string text2 = "res://ActsFromThePast/bgm/" + text + ".ogg";
		if (_currentMusicPath == text2)
		{
			AudioStreamPlayer? musicPlayer = _musicPlayer;
			if (musicPlayer != null && musicPlayer.Playing)
			{
				return;
			}
		}
		StopMusic();
		AudioStream val = GD.Load<AudioStream>(text2);
		if (val != null)
		{
			AudioStreamOggVorbis val2 = (AudioStreamOggVorbis)(object)((val is AudioStreamOggVorbis) ? val : null);
			if (val2 != null)
			{
				val2.Loop = true;
			}
			_musicPlayer = new AudioStreamPlayer();
			_musicPlayer.Stream = val;
			_musicPlayer.Bus = StringName.op_Implicit("Master");
			_currentVolumeOffset = volumeDbOffset;
			float volumeBgm = SaveManager.Instance.SettingsSave.VolumeBgm;
			_musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volumeBgm, 2f)) + _currentVolumeOffset + -3f;
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				((Node)instance).AddChild((Node)(object)_musicPlayer, false, (InternalMode)0);
				_musicPlayer.Play(0f);
				_currentMusicPath = text2;
			}
		}
	}

	public static void SetMusicVolume(float volume)
	{
		if (_musicPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_musicPlayer))
		{
			_musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volume, 2f)) + _currentVolumeOffset + -3f;
		}
	}

	public static void FadeIn(string[] musicOptions, float duration = 1f, float volumeDbOffset = 0f)
	{
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		if (musicOptions == null || musicOptions.Length == 0)
		{
			return;
		}
		string text = musicOptions[GD.RandRange(0, musicOptions.Length - 1)];
		string text2 = "res://ActsFromThePast/bgm/" + text + ".ogg";
		if (_currentMusicPath == text2)
		{
			AudioStreamPlayer? musicPlayer = _musicPlayer;
			if (musicPlayer != null && musicPlayer.Playing)
			{
				return;
			}
		}
		if (_musicPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_musicPlayer))
		{
			Tween? outgoingFadeTween = _outgoingFadeTween;
			if (outgoingFadeTween != null)
			{
				outgoingFadeTween.Kill();
			}
			AudioStreamPlayer? outgoingPlayer = _outgoingPlayer;
			if (outgoingPlayer != null)
			{
				((Node)outgoingPlayer).QueueFree();
			}
			_outgoingPlayer = _musicPlayer;
			_outgoingFadeTween = ((Node)_outgoingPlayer).CreateTween();
			_outgoingFadeTween.TweenProperty((GodotObject)(object)_outgoingPlayer, NodePath.op_Implicit("volume_db"), Variant.op_Implicit(-80f), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)0);
			_outgoingFadeTween.TweenCallback(Callable.From((Action)delegate
			{
				AudioStreamPlayer? outgoingPlayer2 = _outgoingPlayer;
				if (outgoingPlayer2 != null)
				{
					((Node)outgoingPlayer2).QueueFree();
				}
				_outgoingPlayer = null;
			}));
		}
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		_musicPlayer = null;
		_currentMusicPath = null;
		AudioStream val = GD.Load<AudioStream>(text2);
		if (val != null)
		{
			AudioStreamOggVorbis val2 = (AudioStreamOggVorbis)(object)((val is AudioStreamOggVorbis) ? val : null);
			if (val2 != null)
			{
				val2.Loop = true;
			}
			_musicPlayer = new AudioStreamPlayer();
			_musicPlayer.Stream = val;
			_musicPlayer.Bus = StringName.op_Implicit("Master");
			_musicPlayer.VolumeDb = -80f;
			_currentVolumeOffset = volumeDbOffset;
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				((Node)instance).AddChild((Node)(object)_musicPlayer, false, (InternalMode)0);
				_musicPlayer.Play(0f);
				_currentMusicPath = text2;
				float num = Mathf.LinearToDb(Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeBgm, 2f)) + _currentVolumeOffset + -3f;
				_fadeTween = ((Node)_musicPlayer).CreateTween();
				_fadeTween.TweenProperty((GodotObject)(object)_musicPlayer, NodePath.op_Implicit("volume_db"), Variant.op_Implicit(num), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)1);
			}
		}
	}

	public static void FadeOut(float duration = 1f)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (_musicPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_musicPlayer))
		{
			Tween? fadeTween = _fadeTween;
			if (fadeTween != null)
			{
				fadeTween.Kill();
			}
			_fadeTween = ((Node)_musicPlayer).CreateTween();
			_fadeTween.TweenProperty((GodotObject)(object)_musicPlayer, NodePath.op_Implicit("volume_db"), Variant.op_Implicit(-80f), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)0);
			_fadeTween.TweenCallback(Callable.From((Action)delegate
			{
				StopMusicImmediate();
			}));
		}
	}

	private static void StopMusicImmediate()
	{
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		_fadeTween = null;
		Tween? outgoingFadeTween = _outgoingFadeTween;
		if (outgoingFadeTween != null)
		{
			outgoingFadeTween.Kill();
		}
		_outgoingFadeTween = null;
		if (_musicPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_musicPlayer))
		{
			_musicPlayer.Stop();
			((Node)_musicPlayer).QueueFree();
		}
		_musicPlayer = null;
		_currentMusicPath = null;
		if (_outgoingPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_outgoingPlayer))
		{
			_outgoingPlayer.Stop();
			((Node)_outgoingPlayer).QueueFree();
		}
		_outgoingPlayer = null;
	}

	public static void StopMusic()
	{
		StopMusicImmediate();
	}

	public static bool IsPlayingLegacyMusic()
	{
		AudioStreamPlayer? musicPlayer = _musicPlayer;
		return musicPlayer != null && musicPlayer.Playing;
	}

	public static void PlayAmbience(string ambienceName, float volumeDbOffset = 0f)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		string text = "res://ActsFromThePast/bgm/" + ambienceName + ".ogg";
		if (_currentAmbiencePath == text)
		{
			AudioStreamPlayer? ambiencePlayer = _ambiencePlayer;
			if (ambiencePlayer != null && ambiencePlayer.Playing)
			{
				return;
			}
		}
		StopAmbience();
		AudioStream val = GD.Load<AudioStream>(text);
		if (val != null)
		{
			AudioStreamOggVorbis val2 = (AudioStreamOggVorbis)(object)((val is AudioStreamOggVorbis) ? val : null);
			if (val2 != null)
			{
				val2.Loop = true;
			}
			_ambiencePlayer = new AudioStreamPlayer();
			_ambiencePlayer.Stream = val;
			_ambiencePlayer.Bus = StringName.op_Implicit("Master");
			float volumeAmbience = SaveManager.Instance.SettingsSave.VolumeAmbience;
			_ambiencePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volumeAmbience, 2f)) + volumeDbOffset + -6f;
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				((Node)instance).AddChild((Node)(object)_ambiencePlayer, false, (InternalMode)0);
				_ambiencePlayer.Play(0f);
				_currentAmbiencePath = text;
			}
		}
	}

	public static void FadeInAmbience(string ambienceName, float duration = 1f, float volumeDbOffset = 0f)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		string text = "res://ActsFromThePast/bgm/" + ambienceName + ".ogg";
		if (_currentAmbiencePath == text)
		{
			AudioStreamPlayer? ambiencePlayer = _ambiencePlayer;
			if (ambiencePlayer != null && ambiencePlayer.Playing)
			{
				return;
			}
		}
		StopAmbience();
		AudioStream val = GD.Load<AudioStream>(text);
		if (val != null)
		{
			AudioStreamOggVorbis val2 = (AudioStreamOggVorbis)(object)((val is AudioStreamOggVorbis) ? val : null);
			if (val2 != null)
			{
				val2.Loop = true;
			}
			_ambiencePlayer = new AudioStreamPlayer();
			_ambiencePlayer.Stream = val;
			_ambiencePlayer.Bus = StringName.op_Implicit("Master");
			_ambiencePlayer.VolumeDb = -80f;
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				((Node)instance).AddChild((Node)(object)_ambiencePlayer, false, (InternalMode)0);
				_ambiencePlayer.Play(0f);
				_currentAmbiencePath = text;
				float num = Mathf.LinearToDb(Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeAmbience, 2f)) + volumeDbOffset + -6f;
				_ambienceFadeTween = ((Node)_ambiencePlayer).CreateTween();
				_ambienceFadeTween.TweenProperty((GodotObject)(object)_ambiencePlayer, NodePath.op_Implicit("volume_db"), Variant.op_Implicit(num), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)1);
			}
		}
	}

	public static void FadeOutAmbience(float duration = 1f)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (_ambiencePlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_ambiencePlayer))
		{
			Tween? ambienceFadeTween = _ambienceFadeTween;
			if (ambienceFadeTween != null)
			{
				ambienceFadeTween.Kill();
			}
			_ambienceFadeTween = ((Node)_ambiencePlayer).CreateTween();
			_ambienceFadeTween.TweenProperty((GodotObject)(object)_ambiencePlayer, NodePath.op_Implicit("volume_db"), Variant.op_Implicit(-80f), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)0);
			_ambienceFadeTween.TweenCallback(Callable.From((Action)delegate
			{
				StopAmbience();
			}));
		}
	}

	public static void StopAmbience()
	{
		Tween? ambienceFadeTween = _ambienceFadeTween;
		if (ambienceFadeTween != null)
		{
			ambienceFadeTween.Kill();
		}
		_ambienceFadeTween = null;
		if (_ambiencePlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_ambiencePlayer))
		{
			_ambiencePlayer.Stop();
			((Node)_ambiencePlayer).QueueFree();
		}
		_ambiencePlayer = null;
		_currentAmbiencePath = null;
	}

	public static void SetAmbienceVolume(float volume)
	{
		if (_ambiencePlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_ambiencePlayer))
		{
			_ambiencePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volume, 2f)) + -6f;
		}
	}

	public static void PlayBossStinger(float seekFrom = 0f)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		string text = BossStingers[GD.RandRange(0, BossStingers.Length - 1)];
		string text2 = "res://ActsFromThePast/bgm/" + text + ".ogg";
		AudioStream val = GD.Load<AudioStream>(text2);
		if (val != null)
		{
			AudioStreamOggVorbis val2 = (AudioStreamOggVorbis)(object)((val is AudioStreamOggVorbis) ? val : null);
			if (val2 != null)
			{
				val2.Loop = false;
			}
			_musicPlayer = new AudioStreamPlayer();
			_musicPlayer.Stream = val;
			_musicPlayer.Bus = StringName.op_Implicit("Master");
			float volumeBgm = SaveManager.Instance.SettingsSave.VolumeBgm;
			_musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volumeBgm, 2f)) + -3f;
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				((Node)instance).AddChild((Node)(object)_musicPlayer, false, (InternalMode)0);
				_musicPlayer.Play(seekFrom);
				_currentMusicPath = text2;
			}
		}
	}
}
