using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;

namespace Downfall.DownfallCode.Utils;

public static class CombatUiHooks
{
	private static readonly List<Action<CombatState>> handlers = new List<Action<CombatState>>();

	public static void Register(Action<CombatState> handler)
	{
		handlers.Add(handler);
	}

	internal static void RaiseActivate(CombatState state)
	{
		foreach (Action<CombatState> handler in handlers)
		{
			try
			{
				handler(state);
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"CombatUi activate handler failed: {value}", 1);
			}
		}
	}
}
