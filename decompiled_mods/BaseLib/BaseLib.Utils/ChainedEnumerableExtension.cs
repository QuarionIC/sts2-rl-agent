using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BaseLib.Utils;

public static class ChainedEnumerableExtension
{
	public static ChainedEnumerable<T> Chain<T>(this IEnumerable<T> enumerable, [ParamCollection] IEnumerable<T> additional)
	{
		return new ChainedEnumerable<T>(enumerable, additional);
	}
}
