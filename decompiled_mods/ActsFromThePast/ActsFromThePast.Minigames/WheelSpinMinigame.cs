using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class WheelSpinMinigame
{
	private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();

	private readonly Player _owner;

	public int Result { get; }

	public float ResultAngle { get; }

	public int ActIndex { get; }

	public event Action? Finished;

	public WheelSpinMinigame(Player owner, int result, int actIndex)
	{
		_owner = owner;
		Result = result;
		ActIndex = actIndex;
		ResultAngle = (float)result * 60f + (float)Rng.Chaotic.NextInt(-10, 11);
	}

	public void Complete()
	{
		if (!_completionSource.Task.IsCompleted)
		{
			_completionSource.SetResult();
			this.Finished?.Invoke();
		}
	}

	public void ForceEnd()
	{
		_completionSource.TrySetCanceled();
	}

	public async Task PlayMinigame()
	{
		if (LocalContext.IsMe(_owner))
		{
			NWheelSpinScreen.ShowScreen(this);
			await _completionSource.Task;
		}
	}
}
