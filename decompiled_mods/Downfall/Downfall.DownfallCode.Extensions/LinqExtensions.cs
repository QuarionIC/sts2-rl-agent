using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Downfall.DownfallCode.Extensions;

public static class LinqExtensions
{
	public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
	{
		foreach (T item in source.ToList())
		{
			await action(item);
		}
	}
}
