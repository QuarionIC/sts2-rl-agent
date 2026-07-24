using System.Collections.Generic;
using BaseLib.Audio;
using Downfall.DownfallCode.Utils.Sound;

namespace Downfall.DownfallCode.Patches;

public static class SfxOverrideRegistry
{
	private static readonly Dictionary<string, ModSoundEffect> Overrides = new Dictionary<string, ModSoundEffect>();

	public static void Register(string path, ModSoundEffect effect)
	{
		Overrides[path] = effect;
	}

	public static bool TryHandleResPath(string path)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if (!path.StartsWith("res://"))
		{
			return false;
		}
		ModSoundEffect valueOrDefault = Overrides.GetValueOrDefault(path);
		if (valueOrDefault != null)
		{
			valueOrDefault.Play();
			return true;
		}
		ModAudio.PlaySoundGlobal(new ModSound(path, (SoundType)0), 0f, 1f, 0f, 1f);
		return true;
	}
}
