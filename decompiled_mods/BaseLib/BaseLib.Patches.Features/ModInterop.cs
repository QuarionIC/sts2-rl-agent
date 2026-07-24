using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BaseLib.Extensions;
using BaseLib.Utils;
using BaseLib.Utils.ModInterop;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace BaseLib.Patches.Features;

internal class ModInterop
{
	private static class QuickTranspiler
	{
		public static List<CodeInstruction> Insert = new List<CodeInstruction>();

		public static List<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
		{
			return new InstructionPatcher(instructions).Match(new InstructionMatcher().ret()).Step(-1).Insert((IEnumerable<CodeInstruction>)Insert);
		}
	}

	private static readonly BindingFlags ValidMemberFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

	private static readonly FieldInfo WrappedValueField = AccessTools.DeclaredField(typeof(InteropClassWrapper), "Value");

	private readonly Dictionary<string, List<Assembly>> _loadedIds;

	internal ModInterop()
	{
		BaseLibMain.Logger.Info("Generating interop methods and properties", 1);
		_loadedIds = (from mod in ModManager.GetLoadedMods()
			where mod.manifest?.id != null
			select mod).ToDictionary((Mod mod) => mod.manifest?.id ?? "", WhatMod.AssembliesForMod);
	}

	internal void ProcessType(Harmony harmony, Type t)
	{
		ModInteropAttribute customAttribute = t.GetCustomAttribute<ModInteropAttribute>();
		if (customAttribute != null && _loadedIds.TryGetValue(customAttribute.ModId, out List<Assembly> value))
		{
			if (value.Count == 0)
			{
				BaseLibMain.Logger.Error("Cannot generate interop for mod " + customAttribute.ModId + ", no assemblies found", 1);
				return;
			}
			BaseLibMain.Logger.Info($"Interop type {t} for mod {customAttribute.ModId}", 1);
			GenInteropMembers(t.GetMembers(ValidMemberFlags), harmony, value, customAttribute.Type, requireStatic: true);
		}
	}

	private static bool GenInteropMembers(MemberInfo[] members, Harmony harmony, List<Assembly> assemblies, string? contextTargetType, bool requireStatic)
	{
		foreach (MemberInfo memberInfo in members)
		{
			if (!(memberInfo is PropertyInfo propertyInfo))
			{
				if (!(memberInfo is MethodInfo methodInfo))
				{
					if (memberInfo is TypeInfo typeInfo && typeInfo.IsAssignableTo(typeof(InteropClassWrapper)) && !GenInteropType(harmony, assemblies, contextTargetType, typeInfo))
					{
						return false;
					}
				}
				else if ((!requireStatic || methodInfo.IsStatic) && !methodInfo.IsConstructor && methodInfo.GetCustomAttribute<CompilerGeneratedAttribute>() == null && !GenInteropMethod(harmony, assemblies, contextTargetType, methodInfo))
				{
					return false;
				}
				continue;
			}
			if (requireStatic)
			{
				MethodInfo? setMethod = propertyInfo.SetMethod;
				if ((object)setMethod != null && !setMethod.IsStatic)
				{
					continue;
				}
			}
			if (GenInteropPropertyOrField(harmony, assemblies, contextTargetType, propertyInfo))
			{
				continue;
			}
			return false;
		}
		return true;
	}

	private static bool GenInteropType(Harmony harmony, List<Assembly> assemblies, string? contextTargetType, TypeInfo type)
	{
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Expected O, but got Unknown
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Expected O, but got Unknown
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Expected O, but got Unknown
		ConstructorInfo[] constructors = type.GetConstructors();
		if (constructors.Length < 1)
		{
			throw new Exception($"{type} must have at least one public constructor");
		}
		InteropTargetAttribute customAttribute = type.GetCustomAttribute<InteropTargetAttribute>();
		string text = customAttribute?.Type ?? customAttribute?.Name ?? contextTargetType ?? throw new Exception($"No target type provided for Interop type {type}");
		try
		{
			Type type2 = null;
			foreach (Assembly assembly in assemblies)
			{
				type2 = Type.GetType($"{text}, {assembly}");
				if (type2 != null)
				{
					break;
				}
			}
			if (type2 == null)
			{
				BaseLibMain.Logger.Error("Failed to generate interop type; Type " + text + " not found in assemblies " + assemblies.AsReadable(), 1);
				return false;
			}
			ConstructorInfo[] array = constructors;
			foreach (ConstructorInfo constructorInfo in array)
			{
				Type[] array2 = (from p in constructorInfo.GetParameters()
					select p.ParameterType).ToArray();
				ConstructorInfo constructor = type2.GetConstructor(array2);
				if (constructor == null)
				{
					throw new Exception("Failed to find matching constructor for " + GeneralExtensions.FullDescription((MethodBase)constructorInfo));
				}
				List<CodeInstruction> list = new List<CodeInstruction>();
				list.Add(CodeInstruction.LoadArgument(0, false));
				list.AddRange(from param in array2.Index()
					select CodeInstruction.LoadArgument(param.Index + 1, false));
				list.Add(new CodeInstruction(OpCodes.Newobj, (object)constructor));
				list.Add(new CodeInstruction(OpCodes.Stfld, (object)WrappedValueField));
				QuickTranspiler.Insert = list;
				harmony.Patch((MethodBase)constructorInfo, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
			}
			BaseLibMain.Logger.Info("Generated interop type " + type.FullName, 1);
			return GenInteropMembers(type.GetMembers(ValidMemberFlags), harmony, assemblies, text, requireStatic: false);
		}
		catch (Exception ex)
		{
			BaseLibMain.Logger.Info(ex.ToString(), 1);
			return false;
		}
	}

	private static bool GenInteropMethod(Harmony harmony, List<Assembly> assemblies, string? contextTargetType, MethodInfo method)
	{
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Expected O, but got Unknown
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Expected O, but got Unknown
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Expected O, but got Unknown
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Expected O, but got Unknown
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Expected O, but got Unknown
		InteropTargetAttribute? customAttribute = method.GetCustomAttribute<InteropTargetAttribute>();
		string text = customAttribute?.Type ?? contextTargetType ?? throw new Exception("Mod interop " + GeneralExtensions.FullDescription((MethodBase)method) + " does not define target type");
		string text2 = customAttribute?.Name ?? method.Name;
		try
		{
			Type type = null;
			foreach (Assembly assembly in assemblies)
			{
				type = Type.GetType($"{text}, {assembly}");
				if (type != null)
				{
					break;
				}
			}
			if (type == null)
			{
				BaseLibMain.Logger.Error("Failed to generate interop type; Type " + text + " not found in assemblies " + assemblies.AsReadable(), 1);
				return false;
			}
			Type[] array = (from p in method.GetParameters()
				select p.ParameterType).ToArray();
			Type[] array2 = (method.IsStatic ? array.Skip(1).ToArray() : array);
			MethodInfo methodInfo = null;
			List<CodeInstruction> list = new List<CodeInstruction>();
			foreach (MethodInfo declaredMethod in AccessToolsExtensions.GetDeclaredMethods(type))
			{
				if (declaredMethod.Name != text2)
				{
					continue;
				}
				ParameterInfo[] parameters = declaredMethod.GetParameters();
				Type[] checkParams = (declaredMethod.IsStatic ? array : array2);
				if (!CheckParamMatch(parameters, checkParams))
				{
					continue;
				}
				methodInfo = declaredMethod;
				if (!methodInfo.IsStatic && method.IsStatic)
				{
					throw new Exception($"Method {method} should not be static to match target {methodInfo}");
				}
				if (methodInfo.ReturnType != typeof(void))
				{
					list.Add(new CodeInstruction(OpCodes.Pop, (object)null));
				}
				int num = 0;
				if (!methodInfo.IsStatic)
				{
					if (method.IsStatic)
					{
						list.Add(CodeInstruction.LoadArgument(0, false));
						if (array[0] != type)
						{
							list.Add(new CodeInstruction(OpCodes.Castclass, (object)type));
						}
						num++;
					}
					else
					{
						list.Add(CodeInstruction.LoadArgument(0, false));
						list.Add(new CodeInstruction(OpCodes.Ldfld, (object)WrappedValueField));
					}
				}
				for (int num2 = 0; num2 < parameters.Length; num2++)
				{
					list.Add(CodeInstruction.LoadArgument(num2 + num, false));
					if (array[num2 + num] != parameters[num2].ParameterType)
					{
						list.Add(new CodeInstruction(OpCodes.Castclass, (object)parameters[num2].ParameterType));
					}
				}
				break;
			}
			if (methodInfo == null)
			{
				throw new Exception($"Method {text2} with matching parameters not found in type {type}");
			}
			if (methodInfo.ReturnType != method.ReturnType)
			{
				throw new Exception($"Method {text2} return type {method.ReturnType} does not match target method return type {methodInfo.ReturnType}");
			}
			List<CodeInstruction> list2 = list;
			int num3 = 1 + list2.Count;
			List<CodeInstruction> list3 = new List<CodeInstruction>(num3);
			CollectionsMarshal.SetCount(list3, num3);
			Span<CodeInstruction> span = CollectionsMarshal.AsSpan(list3);
			int num4 = 0;
			Span<CodeInstruction> span2 = CollectionsMarshal.AsSpan(list2);
			span2.CopyTo(span.Slice(num4, span2.Length));
			num4 += span2.Length;
			span[num4] = new CodeInstruction(OpCodes.Call, (object)methodInfo);
			QuickTranspiler.Insert = list3;
			harmony.Patch((MethodBase)method, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
			BaseLibMain.Logger.Info("Generated interop method " + method.Name, 1);
		}
		catch (Exception ex)
		{
			BaseLibMain.Logger.Info(ex.ToString(), 1);
			return false;
		}
		return true;
	}

	private static bool GenInteropPropertyOrField(Harmony harmony, List<Assembly> assemblies, string? contextTargetType, PropertyInfo property)
	{
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0783: Expected O, but got Unknown
		//IL_079c: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a2: Expected O, but got Unknown
		//IL_07b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bd: Expected O, but got Unknown
		//IL_07d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Expected O, but got Unknown
		//IL_0725: Unknown result type (might be due to invalid IL or missing references)
		//IL_072b: Expected O, but got Unknown
		//IL_0741: Unknown result type (might be due to invalid IL or missing references)
		//IL_0747: Expected O, but got Unknown
		//IL_0802: Unknown result type (might be due to invalid IL or missing references)
		//IL_080d: Expected O, but got Unknown
		//IL_089b: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Expected O, but got Unknown
		//IL_08b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bc: Expected O, but got Unknown
		//IL_08d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08db: Expected O, but got Unknown
		//IL_08f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f7: Expected O, but got Unknown
		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
		//IL_0849: Expected O, but got Unknown
		//IL_085f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0865: Expected O, but got Unknown
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Expected O, but got Unknown
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Expected O, but got Unknown
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Expected O, but got Unknown
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Expected O, but got Unknown
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Expected O, but got Unknown
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Expected O, but got Unknown
		//IL_0920: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Expected O, but got Unknown
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Expected O, but got Unknown
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Expected O, but got Unknown
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Expected O, but got Unknown
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Expected O, but got Unknown
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Expected O, but got Unknown
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Expected O, but got Unknown
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Expected O, but got Unknown
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Expected O, but got Unknown
		InteropTargetAttribute? customAttribute = property.GetCustomAttribute<InteropTargetAttribute>();
		string text = customAttribute?.Type ?? contextTargetType ?? throw new Exception($"Mod interop {property} does not define target type");
		string text2 = customAttribute?.Name ?? property.Name;
		try
		{
			Type type = null;
			foreach (Assembly assembly in assemblies)
			{
				type = Type.GetType($"{text}, {assembly}");
				if (type != null)
				{
					break;
				}
			}
			if (type == null)
			{
				BaseLibMain.Logger.Error("Failed to generate interop type; Type " + text + " not found in assemblies " + assemblies.AsReadable(), 1);
				return false;
			}
			PropertyInfo propertyInfo = AccessToolsExtensions.DeclaredProperty(type, text2);
			if (propertyInfo != null && propertyInfo.PropertyType == property.PropertyType)
			{
				if (propertyInfo.SetMethod == null && propertyInfo.GetMethod == null)
				{
					throw new Exception($"Cannot get or set target property {propertyInfo}");
				}
				MethodInfo? setMethod = propertyInfo.SetMethod;
				bool flag = ((object)setMethod != null && setMethod.IsStatic) || (propertyInfo.GetMethod?.IsStatic ?? false);
				MethodInfo? setMethod2 = property.SetMethod;
				bool flag2 = ((object)setMethod2 != null && setMethod2.IsStatic) || (property.GetMethod?.IsStatic ?? false);
				if (flag && !flag2)
				{
					throw new Exception($"Target property {propertyInfo} is static; interop property must also be static");
				}
				if (flag2 && !flag)
				{
					throw new Exception($"Target property {propertyInfo} is not static; interop property should not be static");
				}
				if (propertyInfo.SetMethod != null)
				{
					if (property.SetMethod == null)
					{
						throw new Exception($"Property {property} should have a setter to match target property");
					}
					if (flag)
					{
						int num = 2;
						List<CodeInstruction> list = new List<CodeInstruction>(num);
						CollectionsMarshal.SetCount(list, num);
						Span<CodeInstruction> span = CollectionsMarshal.AsSpan(list);
						int num2 = 0;
						span[num2] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
						num2++;
						span[num2] = new CodeInstruction(OpCodes.Call, (object)propertyInfo.SetMethod);
						QuickTranspiler.Insert = list;
					}
					else
					{
						int num2 = 4;
						List<CodeInstruction> list2 = new List<CodeInstruction>(num2);
						CollectionsMarshal.SetCount(list2, num2);
						Span<CodeInstruction> span2 = CollectionsMarshal.AsSpan(list2);
						int num = 0;
						span2[num] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
						num++;
						span2[num] = new CodeInstruction(OpCodes.Ldfld, (object)WrappedValueField);
						num++;
						span2[num] = new CodeInstruction(OpCodes.Ldarg_1, (object)null);
						num++;
						span2[num] = new CodeInstruction(OpCodes.Call, (object)propertyInfo.SetMethod);
						QuickTranspiler.Insert = list2;
					}
					harmony.Patch((MethodBase)property.SetMethod, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
				}
				if (propertyInfo.GetMethod != null)
				{
					if (property.GetMethod == null)
					{
						throw new Exception($"Property {property} should have a getter to match target property");
					}
					if (flag)
					{
						int num = 2;
						List<CodeInstruction> list3 = new List<CodeInstruction>(num);
						CollectionsMarshal.SetCount(list3, num);
						Span<CodeInstruction> span3 = CollectionsMarshal.AsSpan(list3);
						int num2 = 0;
						span3[num2] = new CodeInstruction(OpCodes.Pop, (object)null);
						num2++;
						span3[num2] = new CodeInstruction(OpCodes.Call, (object)propertyInfo.GetMethod);
						QuickTranspiler.Insert = list3;
					}
					else
					{
						int num2 = 4;
						List<CodeInstruction> list4 = new List<CodeInstruction>(num2);
						CollectionsMarshal.SetCount(list4, num2);
						Span<CodeInstruction> span4 = CollectionsMarshal.AsSpan(list4);
						int num = 0;
						span4[num] = new CodeInstruction(OpCodes.Pop, (object)null);
						num++;
						span4[num] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
						num++;
						span4[num] = new CodeInstruction(OpCodes.Ldfld, (object)WrappedValueField);
						num++;
						span4[num] = new CodeInstruction(OpCodes.Call, (object)propertyInfo.GetMethod);
						QuickTranspiler.Insert = list4;
					}
					harmony.Patch((MethodBase)property.GetMethod, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
				}
				BaseLibMain.Logger.Info("Generated interop property " + property.Name, 1);
				return true;
			}
			FieldInfo fieldInfo = AccessToolsExtensions.DeclaredField(type, text2);
			if (fieldInfo != null && fieldInfo.FieldType == property.PropertyType)
			{
				if (property.SetMethod == null)
				{
					throw new Exception($"Interop property {property} should have a setter for field {fieldInfo}");
				}
				if (property.GetMethod == null)
				{
					throw new Exception($"Interop property {property} should have a getter for field {fieldInfo}");
				}
				MethodInfo? setMethod3 = property.SetMethod;
				bool flag3 = ((object)setMethod3 != null && setMethod3.IsStatic) || (property.GetMethod?.IsStatic ?? false);
				if (fieldInfo.IsStatic && !flag3)
				{
					throw new Exception($"Target field {fieldInfo} is static; interop property must also be static");
				}
				if (flag3 && !fieldInfo.IsStatic)
				{
					throw new Exception($"Target field {fieldInfo} is not static; interop property should not be static");
				}
				if (fieldInfo.IsStatic)
				{
					int num = 2;
					List<CodeInstruction> list5 = new List<CodeInstruction>(num);
					CollectionsMarshal.SetCount(list5, num);
					Span<CodeInstruction> span5 = CollectionsMarshal.AsSpan(list5);
					int num2 = 0;
					span5[num2] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
					num2++;
					span5[num2] = new CodeInstruction(OpCodes.Stfld, (object)fieldInfo);
					QuickTranspiler.Insert = list5;
				}
				else
				{
					int num2 = 4;
					List<CodeInstruction> list6 = new List<CodeInstruction>(num2);
					CollectionsMarshal.SetCount(list6, num2);
					Span<CodeInstruction> span6 = CollectionsMarshal.AsSpan(list6);
					int num = 0;
					span6[num] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
					num++;
					span6[num] = new CodeInstruction(OpCodes.Ldfld, (object)WrappedValueField);
					num++;
					span6[num] = new CodeInstruction(OpCodes.Ldarg_1, (object)null);
					num++;
					span6[num] = new CodeInstruction(OpCodes.Stfld, (object)fieldInfo);
					QuickTranspiler.Insert = list6;
				}
				harmony.Patch((MethodBase)property.SetMethod, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
				if (fieldInfo.IsStatic)
				{
					int num = 2;
					List<CodeInstruction> list7 = new List<CodeInstruction>(num);
					CollectionsMarshal.SetCount(list7, num);
					Span<CodeInstruction> span7 = CollectionsMarshal.AsSpan(list7);
					int num2 = 0;
					span7[num2] = new CodeInstruction(OpCodes.Pop, (object)null);
					num2++;
					span7[num2] = new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo);
					QuickTranspiler.Insert = list7;
				}
				else
				{
					int num2 = 4;
					List<CodeInstruction> list8 = new List<CodeInstruction>(num2);
					CollectionsMarshal.SetCount(list8, num2);
					Span<CodeInstruction> span8 = CollectionsMarshal.AsSpan(list8);
					int num = 0;
					span8[num] = new CodeInstruction(OpCodes.Pop, (object)null);
					num++;
					span8[num] = new CodeInstruction(OpCodes.Ldarg_0, (object)null);
					num++;
					span8[num] = new CodeInstruction(OpCodes.Ldfld, (object)WrappedValueField);
					num++;
					span8[num] = new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo);
					QuickTranspiler.Insert = list8;
				}
				harmony.Patch((MethodBase)property.GetMethod, (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, List<CodeInstruction>>(QuickTranspiler.Transpile)), (HarmonyMethod)null);
				BaseLibMain.Logger.Info("Generated interop field property " + property.Name, 1);
				return true;
			}
			throw new Exception("Could not find property or field for name " + text2 + " in type " + text);
		}
		catch (Exception ex)
		{
			BaseLibMain.Logger.Info(ex.ToString(), 1);
			return false;
		}
	}

	private static bool CheckParamMatch(ParameterInfo[] targetParams, Type[] checkParams)
	{
		if (targetParams.Length != checkParams.Length)
		{
			return false;
		}
		return !checkParams.Where((Type t, int i) => t != typeof(object) && !t.IsAssignableTo(targetParams[i].ParameterType)).Any();
	}
}
