using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class RelicModelExtensions
{
	private static readonly Dictionary<ModelId, Func<AncientEventModel, bool>> CanSpawnAtAncient = new Dictionary<ModelId, Func<AncientEventModel, bool>>();

	public static void AddCustomAncientSpawnCondition(this RelicModel model, Func<AncientEventModel, bool> condition)
	{
		if (CanSpawnAtAncient.Remove(((AbstractModel)model).Id))
		{
			BaseLibMain.Logger.Warn($"Custom ancient spawn condition set for relic {((AbstractModel)model).Id} multiple times", 1);
		}
		CanSpawnAtAncient[((AbstractModel)model).Id] = condition;
	}

	public static bool RelicCanSpawnAtCustomAncient(this RelicModel model, AncientEventModel ancient)
	{
		if (!CanSpawnAtAncient.TryGetValue(((AbstractModel)model).Id, out Func<AncientEventModel, bool> value))
		{
			return true;
		}
		return value(ancient);
	}
}
