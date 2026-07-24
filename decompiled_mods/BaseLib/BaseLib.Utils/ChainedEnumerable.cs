using System.Collections;
using System.Collections.Generic;

namespace BaseLib.Utils;

public class ChainedEnumerable<T> : IEnumerable<T>, IEnumerable
{
	private readonly IEnumerable<T>[] _inners;

	public ChainedEnumerable(params IEnumerable<T>[] inners)
	{
		_inners = inners;
	}

	public IEnumerator<T> GetEnumerator()
	{
		IEnumerable<T>[] inners = _inners;
		foreach (IEnumerable<T> enumerable in inners)
		{
			foreach (T item in enumerable)
			{
				yield return item;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
