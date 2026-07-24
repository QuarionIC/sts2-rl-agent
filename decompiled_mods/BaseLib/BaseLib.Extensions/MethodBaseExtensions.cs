using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Extensions;

namespace BaseLib.Extensions;

public static class MethodBaseExtensions
{
	public static int ArgIndex(this MethodBase method, string paramName)
	{
		int num = ListExtensions.FirstIndex<ParameterInfo>((IReadOnlyList<ParameterInfo>)method.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo param) => param.Name == paramName));
		if (num == -1)
		{
			throw new ArgumentException($"Failed to find parameter in method {method.Name} with name {paramName}.");
		}
		return num + ((!method.IsStatic) ? 1 : 0);
	}
}
