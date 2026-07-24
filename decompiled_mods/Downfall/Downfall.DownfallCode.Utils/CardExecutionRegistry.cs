using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Utils;

public static class CardExecutionRegistry
{
	public delegate Task AfterPlayCallback(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay);

	public delegate Task<bool> BeforePlayCallback(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay);

	internal static readonly List<AfterPlayCallback> AfterListeners = new List<AfterPlayCallback>();

	internal static readonly List<BeforePlayCallback> BeforeListeners = new List<BeforePlayCallback>();

	public static void RegisterBefore(BeforePlayCallback callback)
	{
		if (!BeforeListeners.Contains(callback))
		{
			BeforeListeners.Add(callback);
		}
	}

	public static void RegisterAfter(AfterPlayCallback callback)
	{
		if (!AfterListeners.Contains(callback))
		{
			AfterListeners.Add(callback);
		}
	}

	public static async Task<bool> BeforeOnPlayInternal(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (BeforePlayCallback beforeListener in BeforeListeners)
		{
			if (!(await beforeListener(card, choiceContext, cardPlay)))
			{
				return true;
			}
		}
		return false;
	}

	public static async Task AfterOnPlayInternal(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (AfterPlayCallback afterListener in AfterListeners)
		{
			await afterListener(card, choiceContext, cardPlay);
		}
	}
}
