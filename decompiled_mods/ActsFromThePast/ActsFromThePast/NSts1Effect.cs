using Godot;

namespace ActsFromThePast;

public abstract class NSts1Effect : Node2D
{
	protected float Duration;

	protected float StartingDuration;

	protected Color EffectColor = Colors.White;

	public bool IsDone = false;

	protected void Setup()
	{
		((Node)this).ProcessMode = (ProcessModeEnum)3;
		((Node)this).SetProcess(true);
		((Node)this).TreeEntered += OnTreeEntered;
	}

	private void OnTreeEntered()
	{
		Initialize();
		((Node)this).GetTree().ProcessFrame += OnProcessFrame;
	}

	private void OnProcessFrame()
	{
		if (!((Node)this).IsInsideTree())
		{
			((Node)this).GetTree().ProcessFrame -= OnProcessFrame;
			return;
		}
		Update((float)((Node)this).GetProcessDeltaTime());
		if (IsDone)
		{
			((Node)this).GetTree().ProcessFrame -= OnProcessFrame;
			((Node)this).QueueFree();
		}
	}

	protected virtual void Initialize()
	{
	}

	protected abstract void Update(float delta);

	protected static float Lerp(float from, float to, float t)
	{
		return from + (to - from) * t;
	}

	protected static float EaseOut(float t)
	{
		return 1f - (1f - t) * (1f - t);
	}

	protected static float EaseIn(float t)
	{
		return t * t;
	}

	protected static float BounceIn(float t)
	{
		return 1f - BounceOut(1f - t);
	}

	protected static float BounceOut(float t)
	{
		if (t < 0.36363637f)
		{
			return 7.5625f * t * t;
		}
		if (t < 0.72727275f)
		{
			t -= 0.54545456f;
			return 7.5625f * t * t + 0.75f;
		}
		if (t < 0.90909094f)
		{
			t -= 0.8181818f;
			return 7.5625f * t * t + 0.9375f;
		}
		t -= 21f / 22f;
		return 7.5625f * t * t + 63f / 64f;
	}

	protected static float Smootherstep(float t)
	{
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}
}
