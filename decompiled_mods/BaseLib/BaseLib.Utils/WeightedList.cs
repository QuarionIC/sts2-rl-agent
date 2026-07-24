using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;

namespace BaseLib.Utils;

public class WeightedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
{
	private class WeightedItem
	{
		public int Weight { get; }

		public T Val { get; set; }

		public WeightedItem(T val, int weight)
		{
			Weight = weight;
			Val = val;
		}
	}

	private readonly List<WeightedItem> _items = new List<WeightedItem>();

	private int _totalWeight;

	public int Count => _items.Count;

	public bool IsReadOnly => false;

	public T this[int index]
	{
		get
		{
			return _items[index].Val;
		}
		set
		{
			_items[index].Val = value;
		}
	}

	public T GetRandom(Rng rng)
	{
		return GetRandom(rng, remove: false);
	}

	public T GetRandom(Rng rng, bool remove)
	{
		if (Count == 0)
		{
			throw new IndexOutOfRangeException("Attempted to roll on empty WeightedList");
		}
		int num = rng.NextInt(_totalWeight);
		int num2 = 0;
		WeightedItem weightedItem = null;
		foreach (WeightedItem item in _items)
		{
			if (num2 + item.Weight > num)
			{
				weightedItem = item;
				break;
			}
			num2 += item.Weight;
		}
		if (weightedItem != null)
		{
			if (remove)
			{
				_items.Remove(weightedItem);
				_totalWeight -= weightedItem.Weight;
			}
			return weightedItem.Val;
		}
		throw new Exception($"Roll {num} failed to get a value in list of total weight {_totalWeight}");
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _items.Select((WeightedItem item) => item.Val).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(T item)
	{
		Add(item, (!(item is IWeighted weighted)) ? 1 : weighted.Weight);
	}

	public void Add(T item, int weight)
	{
		_totalWeight += weight;
		_items.Add(new WeightedItem(item, weight));
	}

	public void Clear()
	{
		_items.Clear();
		_totalWeight = 0;
	}

	public bool Contains(T val)
	{
		return _items.Any((WeightedItem item) => object.Equals(item.Val, val));
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_items.Select((WeightedItem item) => item.Val).ToList().CopyTo(array, arrayIndex);
	}

	public bool Remove(T val)
	{
		WeightedItem weightedItem = _items.Find((WeightedItem item) => object.Equals(item.Val, val));
		if (weightedItem != null)
		{
			_items.Remove(weightedItem);
			_totalWeight -= weightedItem.Weight;
			return true;
		}
		return false;
	}

	public int IndexOf(T val)
	{
		return ListExtensions.FirstIndex<WeightedItem>((IReadOnlyList<WeightedItem>)_items, (Predicate<WeightedItem>)((WeightedItem item) => object.Equals(item.Val, val)));
	}

	public void Insert(int index, T item)
	{
		Insert(index, item, 1);
	}

	public void Insert(int index, T item, int weight)
	{
		_items.Insert(index, new WeightedItem(item, weight));
		_totalWeight += weight;
	}

	public void RemoveAt(int index)
	{
		WeightedItem weightedItem = _items[index];
		_items.RemoveAt(index);
		_totalWeight -= weightedItem.Weight;
	}
}
