using BaseLib.Audio;

namespace Downfall.DownfallCode.Utils.Sound;

public class ModSoundEntry
{
	public ModSound Sound { get; } = new ModSound(path, (SoundType)0);

	public float Weight { get; }

	public float PitchVariation { get; }

	public float BasePitch { get; }

	public float VolumeAdd { get; }

	public ModSoundEntry(string path, float weight = 1f, float pitchVariation = 0f, float basePitch = 1f, float volumeAdd = 0f)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		Weight = weight;
		PitchVariation = pitchVariation;
		BasePitch = basePitch;
		VolumeAdd = volumeAdd;
		base._002Ector();
	}
}
