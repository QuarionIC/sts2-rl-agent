using System;
using System.Collections.Generic;

namespace BaseLib.Extensions;

public static class ListExtensions
{
	public static void InsertSorted<T>(this List<T> list, T item) where T : IComparable<T>
	{
		if (list.Count != 0)
		{
			if (list[list.Count - 1].CompareTo(item) > 0)
			{
				if (list[0].CompareTo(item) >= 0)
				{
					list.Insert(0, item);
					return;
				}
				int num = list.BinarySearch(item);
				if (num < 0)
				{
					num = ~num;
				}
				list.Insert(num, item);
				return;
			}
		}
		list.Add(item);
	}

	public static void InsertSorted<T>(this List<T> list, T item, IComparer<T> comparer)
	{
		if (list.Count != 0)
		{
			if (comparer.Compare(list[list.Count - 1], item) > 0)
			{
				if (comparer.Compare(list[0], item) >= 0)
				{
					list.Insert(0, item);
					return;
				}
				int num = list.BinarySearch(item, comparer);
				if (num < 0)
				{
					num = ~num;
				}
				list.Insert(num, item);
				return;
			}
		}
		list.Add(item);
	}
}
