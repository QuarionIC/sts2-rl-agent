using System;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ModelExtensions
{
	public static string LocKey(this AbstractModel model, string subKey)
	{
		return model.Id.Entry + "." + subKey;
	}

	public static DynamicVar GetDynamicVar(this AbstractModel model, string varKey)
	{
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val == null)
		{
			RelicModel val2 = (RelicModel)(object)((model is RelicModel) ? model : null);
			if (val2 == null)
			{
				PowerModel val3 = (PowerModel)(object)((model is PowerModel) ? model : null);
				if (val3 != null)
				{
					return val3.DynamicVars[varKey];
				}
				throw new Exception(((object)model).GetType().Name + " does not have dynamic vars (or is unsupported by GetDynamicVar)");
			}
			return val2.DynamicVars[varKey];
		}
		return val.DynamicVars[varKey];
	}
}
