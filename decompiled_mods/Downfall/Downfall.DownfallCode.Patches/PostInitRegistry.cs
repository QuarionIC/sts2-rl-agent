using System;
using System.Collections.Generic;

namespace Downfall.DownfallCode.Patches;

public static class PostInitRegistry
{
	private static readonly List<Action> actions = new List<Action>();

	public static void Register(Action action)
	{
		actions.Add(action);
	}

	internal static void RunAll()
	{
		foreach (Action action in actions)
		{
			try
			{
				action();
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"Post-init action failed: {value}", 1);
			}
		}
	}
}
