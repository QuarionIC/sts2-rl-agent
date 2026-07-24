using System;
using System.Runtime.CompilerServices;
using BaseLib.Abstracts;

namespace Baselib.Patches.Utils;

internal static class SecondAmountRegistry
{
	internal static readonly ConditionalWeakTable<IHasSecondAmount, Action> RefreshActions = new ConditionalWeakTable<IHasSecondAmount, Action>();

	internal static void Register(IHasSecondAmount power, Action refresh)
	{
		RefreshActions.AddOrUpdate(power, refresh);
	}

	internal static void Unregister(IHasSecondAmount power)
	{
		RefreshActions.Remove(power);
	}
}
