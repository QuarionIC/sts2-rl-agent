using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Modding;

namespace Downfall.DownfallCode.Compatibility;

public static class CompatibilityMod
{
	private static readonly Func<Mod, List<Assembly>> _getAssemblies = BuildAccessor();

	public static List<Assembly> GetAssemblies(this Mod mod)
	{
		return _getAssemblies(mod);
	}

	private static Func<Mod, List<Assembly>> BuildAccessor()
	{
		Type typeFromHandle = typeof(Mod);
		MemberInfo multi = (MemberInfo)(((object)typeFromHandle.GetProperty("assemblies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) ?? ((object)typeFromHandle.GetField("assemblies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)));
		if (multi != null)
		{
			return delegate(Mod mod)
			{
				object value = GetValue(multi, mod);
				if (value is List<Assembly> result)
				{
					return result;
				}
				return (value is IEnumerable<Assembly> source) ? source.ToList() : new List<Assembly>();
			};
		}
		MemberInfo single = (MemberInfo)(((object)typeFromHandle.GetProperty("assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) ?? ((object)typeFromHandle.GetField("assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)));
		if (single != null)
		{
			return delegate(Mod mod)
			{
				if (GetValue(single, mod) is Assembly assembly)
				{
					int num = 1;
					List<Assembly> list = new List<Assembly>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = assembly;
					return list;
				}
				return new List<Assembly>();
			};
		}
		throw new MissingMemberException("Mod has neither an 'assemblies' nor an 'assembly' member — unsupported game version.");
	}

	private static object? GetValue(MemberInfo member, Mod mod)
	{
		if (!(member is PropertyInfo propertyInfo))
		{
			if (member is FieldInfo fieldInfo)
			{
				return fieldInfo.GetValue(mod);
			}
			return null;
		}
		return propertyInfo.GetValue(mod);
	}
}
