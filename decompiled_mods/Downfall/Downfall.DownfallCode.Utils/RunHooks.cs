using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace Downfall.DownfallCode.Utils;

public static class RunHooks
{
	private static readonly List<Action<RunState>> newRunHandlers = new List<Action<RunState>>();

	public static void OnNewRun(Action<RunState> handler)
	{
		newRunHandlers.Add(handler);
	}

	public static void OnNewRunPerPlayer(Action<Player> handler)
	{
		newRunHandlers.Add(delegate(RunState state)
		{
			foreach (Player player in state.Players)
			{
				handler(player);
			}
		});
	}

	internal static void RaiseNewRun(RunState state)
	{
		foreach (Action<RunState> newRunHandler in newRunHandlers)
		{
			try
			{
				newRunHandler(state);
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"New-run handler failed: {value}", 1);
			}
		}
	}
}
