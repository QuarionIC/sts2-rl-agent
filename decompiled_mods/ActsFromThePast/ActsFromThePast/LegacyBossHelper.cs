using ActsFromThePast.Patches.Audio;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ActsFromThePast;

public static class LegacyBossHelper
{
	public static void OnBossVictory()
	{
		NRunMusicController instance = NRunMusicController.Instance;
		if (instance != null)
		{
			instance.StopMusic();
		}
		AFTPModAudio.Play("boss", "boss_victory_stinger");
		AFTPModAudio.PlayBossStinger();
		MusicPatches.LegacyActMusicPatches.SetBossStingerState();
	}
}
