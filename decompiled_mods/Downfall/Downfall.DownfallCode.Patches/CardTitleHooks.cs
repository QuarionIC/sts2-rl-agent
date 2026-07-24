using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

public static class CardTitleHooks
{
	private static readonly List<Func<CardModel, string, string>> modifiers = new List<Func<CardModel, string, string>>();

	public static void Register(Func<CardModel, string, string> modifier)
	{
		modifiers.Add(modifier);
	}

	internal static string ApplyModifiers(CardModel card, string title)
	{
		foreach (Func<CardModel, string, string> modifier in modifiers)
		{
			try
			{
				title = modifier(card, title);
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"Title modifier failed: {value}", 1);
			}
		}
		return title;
	}
}
