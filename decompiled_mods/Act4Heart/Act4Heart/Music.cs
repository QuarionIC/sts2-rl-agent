using System;
using Dolso;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MonoMod.Cil;

namespace Act4Heart;

[Hook]
internal static class Music
{
	internal const string BANK_PATH = "res://banks/desktop/Act4Heart/Act4Heart.bank";

	internal const string BACKGROUND_MUSIC = "music/STS_Act4_BGM_v2";

	internal const string BOSS_MUSIC = "music/STS_Boss4_v6";

	private const string PLAYER_NAME = "Act4HeartPlayer";

	private static void on_entered_music(string? path)
	{
		if (path == "event:/music/STS_Act4_BGM_v2")
		{
			start_music("res://music/STS_Act4_BGM_v2.ogg");
		}
		else if (path == "event:/music/STS_Boss4_v6")
		{
			start_music("res://music/STS_Boss4_v6.ogg");
		}
	}

	internal static void stop_music()
	{
		AudioStreamPlayer audio = get_audio();
		if (audio != null && audio.Playing)
		{
			AudioStream stream = audio.Stream;
			log.info("stopped track " + ((stream != null) ? ((Resource)stream).ResourceName : null));
			audio.Stop();
		}
	}

	private static AudioStreamPlayer? get_audio()
	{
		NRun instance = NRun.Instance;
		if (instance == null)
		{
			return null;
		}
		return ((Node)instance).GetNodeOrNull<AudioStreamPlayer>(NodePath.op_Implicit("Act4HeartPlayer"));
	}

	private static AudioStreamPlayer create_audio()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		AudioStreamPlayer val = new AudioStreamPlayer
		{
			Name = StringName.op_Implicit("Act4HeartPlayer"),
			Bus = StringName.op_Implicit("bus:/master/music")
		};
		update_volume(val);
		NRun instance = NRun.Instance;
		if (instance != null)
		{
			((Node)instance).AddChild((Node)(object)val, false, (InternalMode)0);
		}
		return val;
	}

	private static void update_volume(AudioStreamPlayer? audio)
	{
		if (audio != null)
		{
			SettingsSave settingsSave = SaveManager.Instance.SettingsSave;
			audio.VolumeLinear = settingsSave.VolumeMaster * settingsSave.VolumeBgm;
		}
	}

	private static void start_music(string path)
	{
		AudioStreamPlayer? obj = get_audio() ?? create_audio();
		AudioStreamOggVorbis asset = PreloadManager.Cache.GetAsset<AudioStreamOggVorbis>(path);
		asset.Loop = true;
		obj.Stream = (AudioStream)(object)asset;
		obj.Play(0f);
		log.info("started track " + ((Resource)asset).ResourceName);
	}

	[Hook(typeof(NRunMusicController), "StopMusic")]
	[Hook(typeof(NRunMusicController), "UpdateMusic")]
	[Hook(typeof(NRunMusicController), "PlayCustomMusic")]
	[Hook(typeof(NRunMusicController), "StopCustomMusic")]
	[Hook(typeof(NRunMusicController), "UpdateCustomTrack")]
	private static void OnStopMusic(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		new ILCursor(il).EmitDelegate<Action>((Action)func);
		static void func()
		{
			stop_music();
		}
	}

	[HookAfter(typeof(NRunMusicController), "UpdateMusic")]
	private static void AfterStartBgMusic(NRunMusicController __instance)
	{
		if (!NonInteractiveMode.IsActive)
		{
			on_entered_music(__instance._failedTrack ?? __instance._currentTrack);
		}
	}

	[HookAfter(typeof(NRunMusicController), "PlayCustomMusic")]
	private static void AfterStartBossMusic(string customMusic)
	{
		if (!NonInteractiveMode.IsActive)
		{
			on_entered_music(customMusic);
		}
	}

	[HookAfter(typeof(SettingsSaveManager), "SaveSettings")]
	private static void AfterSettingSave()
	{
		update_volume(get_audio());
	}
}
