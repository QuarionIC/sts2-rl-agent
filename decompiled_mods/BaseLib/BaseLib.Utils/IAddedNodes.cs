using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace BaseLib.Utils;

internal interface IAddedNodes<TParentType> where TParentType : Node
{
	protected static List<IAddedNodes<TParentType>> _addedNodes = new List<IAddedNodes<TParentType>>();

	private static bool _patched = false;

	Node? GetNode(TParentType obj);

	protected static void PatchNodeReady()
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		BaseLibMain.Logger.Info("Patching type " + typeof(TParentType).Name + " to add nodes.", 1);
		Harmony mainHarmony = BaseLibMain.MainHarmony;
		MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(TParentType), "_Ready", Array.Empty<Type>(), (Type[])null);
		if (methodInfo != null)
		{
			MethodInfo methodInfo2 = AccessToolsExtensions.DeclaredMethod(typeof(IAddedNodes<TParentType>), "UnconditionalAdd", (Type[])null, (Type[])null);
			BaseLibMain.Logger.Info("Adding postfix " + GeneralExtensions.FullDescription((MethodBase)methodInfo2), 1);
			mainHarmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(methodInfo2), (HarmonyMethod)null, (HarmonyMethod)null);
			return;
		}
		methodInfo = AccessTools.Method(typeof(TParentType), "_Ready", Array.Empty<Type>(), (Type[])null);
		if (methodInfo == null)
		{
			BaseLibMain.Logger.Error("Failed to patch _Ready method for type " + typeof(TParentType).Name + " to add nodes; _Ready method not found.", 1);
			return;
		}
		MethodInfo methodInfo3 = AccessToolsExtensions.DeclaredMethod(typeof(IAddedNodes<TParentType>), "ConditionalAdd", (Type[])null, (Type[])null);
		BaseLibMain.Logger.Info("Adding postfix " + GeneralExtensions.FullDescription((MethodBase)methodInfo3), 1);
		mainHarmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(methodInfo3), (HarmonyMethod)null, (HarmonyMethod)null);
	}

	private static void UnconditionalAdd(TParentType __instance)
	{
		foreach (IAddedNodes<TParentType> addedNode in _addedNodes)
		{
			Node node = addedNode.GetNode(__instance);
			if (((Node)__instance).IsAncestorOf(node))
			{
				break;
			}
			((Node)__instance).AddChild(node, false, (InternalMode)0);
		}
	}

	private static void ConditionalAdd(object __instance)
	{
		TParentType val = (TParentType)((__instance is TParentType) ? __instance : null);
		if (val != null)
		{
			UnconditionalAdd(val);
		}
	}
}
