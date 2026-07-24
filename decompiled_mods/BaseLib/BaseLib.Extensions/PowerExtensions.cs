using System;
using BaseLib.Abstracts;
using Baselib.Patches.Utils;

namespace BaseLib.Extensions;

public static class PowerExtensions
{
	public static void InvokeSecondAmountChanged(this IHasSecondAmount power)
	{
		if (SecondAmountRegistry.RefreshActions.TryGetValue(power, out Action value))
		{
			value();
		}
	}
}
