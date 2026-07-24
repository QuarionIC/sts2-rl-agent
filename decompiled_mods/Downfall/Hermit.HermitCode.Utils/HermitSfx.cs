using BaseLib.Audio;
using Downfall.DownfallCode.Utils.Sound;

namespace Hermit.HermitCode.Utils;

public static class HermitSfx
{
	private const string Gun1 = "res://Hermit/audio/hermit_gun.ogg";

	private const string Gun2 = "res://Hermit/audio/hermit_gun2.ogg";

	private const string Gun3 = "res://Hermit/audio/hermit_gun3.ogg";

	private const string Spin = "res://Hermit/audio/hermit_spin.ogg";

	private const string Reload = "res://Hermit/audio/hermit_reload.ogg";

	private const float DefaultDb = 5f;

	private const float SpinPitchVariation = 0.15f;

	private const float GunPitchVariation = 0.1f;

	public static void PlayGun1(float volumeDb = 5f, float pitchVariation = 0.1f)
	{
		MyModAudio.PlaySound(ModSound.op_Implicit("res://Hermit/audio/hermit_gun.ogg"), volumeDb, 1f, pitchVariation);
	}

	public static void PlayGun2(float volumeDb = 5f, float pitchVariation = 0.1f)
	{
		MyModAudio.PlaySound(ModSound.op_Implicit("res://Hermit/audio/hermit_gun2.ogg"), volumeDb, 1f, pitchVariation);
	}

	public static void PlayGun3(float volumeDb = 5f, float pitchVariation = 0.1f)
	{
		MyModAudio.PlaySound(ModSound.op_Implicit("res://Hermit/audio/hermit_gun3.ogg"), volumeDb, 1f, pitchVariation);
	}

	public static void PlaySpin(float volumeDb = 5f, float pitchVariation = 0.15f)
	{
		MyModAudio.PlaySound(ModSound.op_Implicit("res://Hermit/audio/hermit_spin.ogg"), volumeDb, 1f, pitchVariation);
	}

	public static void PlayReload(float volumeDb = 5f, float pitchVariation = 0.1f)
	{
		MyModAudio.PlaySound(ModSound.op_Implicit("res://Hermit/audio/hermit_reload.ogg"), volumeDb, 1f, pitchVariation);
	}
}
