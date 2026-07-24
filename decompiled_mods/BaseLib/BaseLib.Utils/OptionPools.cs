using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace BaseLib.Utils;

public class OptionPools
{
	private WeightedList<AncientOption>[] _pools;

	public IEnumerable<AncientOption> AllOptions => _pools.SelectMany((WeightedList<AncientOption> pool) => pool);

	public OptionPools(WeightedList<AncientOption> pool1, WeightedList<AncientOption> pool2, WeightedList<AncientOption> pool3)
	{
		_pools = new WeightedList<AncientOption>[3] { pool1, pool2, pool3 };
	}

	public OptionPools(WeightedList<AncientOption> pool12, WeightedList<AncientOption> pool3)
	{
		_pools = new WeightedList<AncientOption>[3] { pool12, pool12, pool3 };
	}

	public OptionPools(WeightedList<AncientOption> pool)
	{
		_pools = new WeightedList<AncientOption>[3] { pool, pool, pool };
	}

	public List<AncientOption> Roll(Rng rng, AncientEventModel ancient)
	{
		List<AncientOption> list = new List<AncientOption>();
		WeightedList<AncientOption> weightedList = _pools[0];
		WeightedList<AncientOption> weightedList2 = new WeightedList<AncientOption>();
		foreach (AncientOption item in weightedList.Where((AncientOption option) => option.ModelForOption.RelicCanSpawnAtCustomAncient(ancient)))
		{
			weightedList2.Add(item);
		}
		WeightedList<AncientOption> weightedList3 = weightedList2;
		list.Add(weightedList3.GetRandom(rng, remove: true));
		if (weightedList != _pools[1])
		{
			weightedList = _pools[1];
			weightedList2 = new WeightedList<AncientOption>();
			foreach (AncientOption item2 in weightedList.Where((AncientOption option) => option.ModelForOption.RelicCanSpawnAtCustomAncient(ancient)))
			{
				weightedList2.Add(item2);
			}
			weightedList3 = weightedList2;
		}
		list.Add(weightedList3.GetRandom(rng, remove: true));
		if (weightedList != _pools[2])
		{
			weightedList = _pools[2];
			weightedList2 = new WeightedList<AncientOption>();
			foreach (AncientOption item3 in weightedList.Where((AncientOption option) => option.ModelForOption.RelicCanSpawnAtCustomAncient(ancient)))
			{
				weightedList2.Add(item3);
			}
			weightedList3 = weightedList2;
		}
		list.Add(weightedList3.GetRandom(rng, remove: true));
		return list;
	}
}
