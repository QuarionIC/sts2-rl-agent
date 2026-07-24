using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Utils;

public static class ForceUpgradeHelper
{
	internal static readonly ConditionalWeakTable<CardModel, StrongBox<int>> ForceUpgraded = new ConditionalWeakTable<CardModel, StrongBox<int>>();

	public static void ForceUpgrade(CardModel card, int times = 1)
	{
		StrongBox<int> orCreateValue = ForceUpgraded.GetOrCreateValue(card);
		for (int i = 0; i < times; i++)
		{
			orCreateValue.Value = card._currentUpgradeLevel + 1;
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
			orCreateValue.Value = card._currentUpgradeLevel;
		}
	}
}
