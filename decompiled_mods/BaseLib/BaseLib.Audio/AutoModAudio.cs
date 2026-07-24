using System.Collections.Generic;
using System.IO;
using Godot;

namespace BaseLib.Audio;

public class AutoModAudio(string folder)
{
	protected readonly string _folder = folder;

	private readonly Dictionary<string, ModSound> _sounds = new Dictionary<string, ModSound>();

	public AudioStreamPlayer? PlaySfx(string path, float volume = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		if (!_sounds.TryGetValue(path, out ModSound value))
		{
			value = new ModSound(ResourceLoader.Exists(path, "") ? path : Path.Join(folder, path));
			_sounds[path] = value;
		}
		return ModAudio.PlaySound(value, volume, volumeMult, pitchVariation, basePitch);
	}

	public AudioStreamPlayer? PlayMusic(string path, float volume = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		if (!_sounds.TryGetValue(path, out ModSound value))
		{
			value = new ModSound(ResourceLoader.Exists(path, "") ? path : Path.Join(folder, path), ModAudio.SoundType.Music);
			_sounds[path] = value;
		}
		return ModAudio.PlaySound(value, volume, volumeMult, pitchVariation, basePitch);
	}

	public AudioStreamPlayer? PlayAmbience(string path, float volume = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		if (!_sounds.TryGetValue(path, out ModSound value))
		{
			value = new ModSound(ResourceLoader.Exists(path, "") ? path : Path.Join(folder, path), ModAudio.SoundType.Ambience);
			_sounds[path] = value;
		}
		return ModAudio.PlaySound(value, volume, volumeMult, pitchVariation, basePitch);
	}
}
