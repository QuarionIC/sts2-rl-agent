using System;
using BaseLib.Utils;
using Godot;

namespace BaseLib.Extensions;

public static class AudioStreamPlayerExtensions
{
	private static readonly NodePath VolumeDb = NodePath.op_Implicit("volume_db");

	public static readonly SpireField<AudioStreamPlayer, Tween> CurrentTween = new SpireField<AudioStreamPlayer, Tween>(() => (Tween?)null);

	public static void FadeIn(this AudioStreamPlayer player, float duration)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		Tween? obj = CurrentTween[player];
		if (obj != null)
		{
			obj.Kill();
		}
		float volumeDb = player.VolumeDb;
		player.VolumeDb = -80f;
		Tween val = ((Node)player).CreateTween();
		val.TweenProperty((GodotObject)(object)player, VolumeDb, Variant.op_Implicit(volumeDb), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)1);
		CurrentTween[player] = val;
	}

	public static void FadeOut(this AudioStreamPlayer player, float duration)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		Tween? obj = CurrentTween[player];
		if (obj != null)
		{
			obj.Kill();
		}
		Tween obj2 = ((Node)player).CreateTween();
		obj2.TweenProperty((GodotObject)(object)player, VolumeDb, Variant.op_Implicit(-80), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)0);
		obj2.TweenCallback(Callable.From((Action)delegate
		{
			Node parent = ((Node)player).GetParent();
			if (parent != null)
			{
				parent.RemoveChild((Node)(object)player);
			}
		}));
		CurrentTween[player] = null;
	}
}
