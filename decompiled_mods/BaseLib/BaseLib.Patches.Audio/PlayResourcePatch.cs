using System;
using System.Collections.Generic;
using BaseLib.Audio;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;

namespace BaseLib.Patches.Audio;

internal static class PlayResourcePatch
{
	[HarmonyPatch(typeof(NAudioManager), "PlayOneShot", new Type[]
	{
		typeof(string),
		typeof(Dictionary<string, float>),
		typeof(float)
	})]
	private static class OneShot
	{
		[HarmonyPrefix]
		private static bool PlayResource(string path, Dictionary<string, float> parameters, float volume)
		{
			if (TestMode.IsOn)
			{
				return true;
			}
			if ((path.StartsWith("res://") || path.StartsWith("user://") || path.StartsWith("uid://")) && ResourceLoader.Exists(path, ""))
			{
				ModAudio.PlaySoundInRun(path, 0f, volume);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(NAudioManager), "PlayMusic")]
	private static class Music
	{
		[HarmonyPrefix]
		private static bool PlayMusic(string music)
		{
			if (TestMode.IsOn)
			{
				return true;
			}
			if ((music.StartsWith("res://") || music.StartsWith("user://") || music.StartsWith("uid://")) && ResourceLoader.Exists(music, ""))
			{
				ModAudio.PlaySoundInRun(new ModSound(music, ModAudio.SoundType.Music));
				return false;
			}
			return true;
		}
	}
}
