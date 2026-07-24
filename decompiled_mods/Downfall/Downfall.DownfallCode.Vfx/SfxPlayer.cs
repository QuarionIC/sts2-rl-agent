using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Downfall.DownfallCode.Vfx;

public static class SfxPlayer
{
	public static void PlaySfx(string path, float pitch = 1f, float volume = 1f)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		if (!NonInteractiveMode.IsActive)
		{
			AudioStream asset;
			try
			{
				asset = PreloadManager.Cache.GetAsset<AudioStream>(path);
			}
			catch
			{
				GD.PrintErr("[SfxPlayer] Could not load audio: " + path);
				return;
			}
			AudioStreamPlayer audioPlayer = new AudioStreamPlayer();
			audioPlayer.Stream = asset;
			audioPlayer.Bus = StringName.op_Implicit("SFX");
			audioPlayer.PitchScale = pitch;
			audioPlayer.VolumeDb = Mathf.LinearToDb(volume);
			audioPlayer.Finished += delegate
			{
				((Node)audioPlayer).QueueFree();
			};
			Node val = (Node)(((object)NCombatRoom.Instance) ?? ((object)NGame.Instance));
			if (val != null)
			{
				val.AddChild((Node)(object)audioPlayer, false, (InternalMode)0);
				audioPlayer.Play(0f);
			}
			else
			{
				((Node)audioPlayer).QueueFree();
			}
		}
	}
}
