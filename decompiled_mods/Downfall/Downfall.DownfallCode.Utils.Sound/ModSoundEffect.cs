using System;
using System.Linq;
using MegaCrit.Sts2.Core.Random;

namespace Downfall.DownfallCode.Utils.Sound;

public class ModSoundEffect
{
	private readonly ModSoundEntry[] _entries;

	private readonly float _globalPitchVariation;

	private readonly float _globalVolumeAdd;

	private readonly float _totalWeight;

	public ModSoundEffect(params ModSoundEntry[] entries)
		: this(0f, 0f, entries)
	{
	}

	public ModSoundEffect(float globalPitchVariation = 0f, float globalVolumeAdd = 0f, params ModSoundEntry[] entries)
	{
		_entries = entries;
		_globalPitchVariation = globalPitchVariation;
		_globalVolumeAdd = globalVolumeAdd;
		_totalWeight = entries.Sum((ModSoundEntry e) => e.Weight);
	}

	public void Play()
	{
		PlayOn(delegate(ModSoundEntry e)
		{
			MyModAudio.PlaySound(e.Sound, _globalVolumeAdd + e.VolumeAdd, 1f, _globalPitchVariation + e.PitchVariation, e.BasePitch);
		});
	}

	public void PlayInRun()
	{
		PlayOn(delegate(ModSoundEntry e)
		{
			MyModAudio.PlaySoundInRun(e.Sound, _globalVolumeAdd + e.VolumeAdd, 1f, _globalPitchVariation + e.PitchVariation, e.BasePitch);
		});
	}

	private void PlayOn(Action<ModSoundEntry> play)
	{
		play(PickRandom());
	}

	private ModSoundEntry PickRandom()
	{
		float num = (float)(Rng.Chaotic.NextDouble() * (double)_totalWeight);
		float num2 = 0f;
		ModSoundEntry[] entries = _entries;
		foreach (ModSoundEntry modSoundEntry in entries)
		{
			num2 += modSoundEntry.Weight;
			if (num <= num2)
			{
				return modSoundEntry;
			}
		}
		return _entries[^1];
	}
}
