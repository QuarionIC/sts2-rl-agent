using System;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

public static class TranscendenceHooks
{
	public static event Action<CardModel, CardModel>? OnTransformed;

	internal static void RaiseTransformed(CardModel starter, CardModel result)
	{
		if (TranscendenceHooks.OnTransformed == null)
		{
			return;
		}
		Delegate[] invocationList = TranscendenceHooks.OnTransformed.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			Action<CardModel, CardModel> action = (Action<CardModel, CardModel>)invocationList[i];
			try
			{
				action(starter, result);
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"Transcendence handler failed: {value}", 1);
			}
		}
	}
}
