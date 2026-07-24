using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace BaseLib.Audio;

public record ModSound
{
	public string File { get; }

	public ModAudio.SoundType SoundType { get; }

	public float VolumeOffset { get; set; }

	private static readonly Dictionary<string, AudioStream> CachedStreams = new Dictionary<string, AudioStream>();

	private static readonly Dictionary<string, float> VolumeOffsets = new Dictionary<string, float>();

	private static readonly Dictionary<string, ModSound> _convertedSounds = new Dictionary<string, ModSound>();

	public static void SetSoundDefaultVolumeOffset(string file, float offset)
	{
		VolumeOffsets[StringExtensions.SimplifyPath(file)] = offset;
	}

	public ModSound(string file, ModAudio.SoundType soundType = ModAudio.SoundType.Sfx)
	{
		File = StringExtensions.SimplifyPath(file);
		SoundType = soundType;
		VolumeOffset = VolumeOffsets.GetValueOrDefault(file, 0f);
	}

	public virtual AudioStream? GetOrLoadStream()
	{
		if (CachedStreams.TryGetValue(File, out AudioStream value))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				return value;
			}
			CachedStreams.Remove(File);
		}
		AudioStream val = GD.Load<AudioStream>(File);
		if (val != null && val.GetLength() < 15.0)
		{
			CachedStreams[File] = val;
		}
		return val;
	}

	public AudioStreamPlayer? Play(float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		return ModAudio.PlaySound(this, volumeAdd + VolumeOffset, volumeMult, pitchVariation, basePitch);
	}

	public static implicit operator ModSound(string path)
	{
		path = StringExtensions.SimplifyPath(path);
		if (!_convertedSounds.TryGetValue(path, out ModSound value))
		{
			value = (_convertedSounds[path] = new ModSound(path));
		}
		return value;
	}

	[CompilerGenerated]
	protected ModSound(ModSound original)
	{
		File = original.File;
		SoundType = original.SoundType;
		VolumeOffset = original.VolumeOffset;
	}
}
