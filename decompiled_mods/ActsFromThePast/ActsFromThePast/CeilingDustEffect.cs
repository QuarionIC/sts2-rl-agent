using System;
using Godot;

namespace ActsFromThePast;

public class CeilingDustEffect : NSts1Effect
{
	private int _count = 20;

	private float _x;

	private Action<NSts1Effect> _addEffectCallback;

	public static CeilingDustEffect Create(Action<NSts1Effect> addEffectCallback)
	{
		CeilingDustEffect ceilingDustEffect = new CeilingDustEffect();
		ceilingDustEffect._addEffectCallback = addEffectCallback;
		ceilingDustEffect.Setup();
		return ceilingDustEffect;
	}

	protected override void Initialize()
	{
		_x = (float)GD.RandRange(0.0, 1870.0);
	}

	protected override void Update(float delta)
	{
		if (_count == 0)
		{
			return;
		}
		int num = (int)(GD.Randi() % 9);
		_count -= num;
		for (int i = 0; i < num; i++)
		{
			FallingDustEffect obj = FallingDustEffect.Create(_x, 100f);
			_addEffectCallback?.Invoke(obj);
			if (GD.Randf() < 0.8f)
			{
				CeilingDustCloudEffect obj2 = CeilingDustCloudEffect.Create(_x, 100f);
				_addEffectCallback?.Invoke(obj2);
			}
		}
		if (_count <= 0)
		{
			IsDone = true;
		}
	}
}
