using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BaseLib.Cards.Variables;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class DynamicVarSetExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_002459D295610ABA3A45E911985519720E87
	{
		[SpecialName]
		public static class _003CM_003E_002447D1F89A39DCAC5A8383DA5CEDFDCBF5
		{
		}

		[ExtensionMarker("<M>$47D1F89A39DCAC5A8383DA5CEDFDCBF5")]
		public DynamicVar Power<T>() where T : PowerModel
		{
			throw null;
		}

		[ExtensionMarker("<M>$47D1F89A39DCAC5A8383DA5CEDFDCBF5")]
		public T Var<T>(string? name = null) where T : DynamicVar
		{
			throw null;
		}
	}

	public static DynamicVar Power<T>(this DynamicVarSet vars) where T : notnull, PowerModel
	{
		return vars[typeof(T).Name];
	}

	public static T Var<T>(this DynamicVarSet vars, string? name = null) where T : notnull, DynamicVar
	{
		if (name != null)
		{
			DynamicVar val = default(DynamicVar);
			if (vars.TryGetValue(name, ref val))
			{
				T val2 = (T)(object)((val is T) ? val : null);
				if (val2 != null)
				{
					return val2;
				}
				throw new ArgumentException($"Found dynamic variable of type {((object)val).GetType().Name} instead of type {typeof(T).Name} with name {name}");
			}
			throw new ArgumentException("Failed to find dynamic variable of type " + typeof(T).Name + " with name " + name);
		}
		return ((IEnumerable<KeyValuePair<string, DynamicVar>>)vars).Select((KeyValuePair<string, DynamicVar> entry) => entry.Value).OfType<T>().FirstOrDefault() ?? throw new ArgumentException("No dynamic variables of type " + typeof(T).Name + " found.");
	}

	public static ScryVar Scry(this DynamicVarSet vard)
	{
		return (ScryVar)(object)vard._vars["Scry"];
	}
}
