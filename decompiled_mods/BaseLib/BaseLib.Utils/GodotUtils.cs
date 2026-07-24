using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Utils;

public static class GodotUtils
{
	[Obsolete("Use NodeFactory<NCreatureVisuals>.CreateFromResource instead.")]
	public static NCreatureVisuals CreatureVisualsFromImage(string path)
	{
		if (!ResourceLoader.Exists(path, ""))
		{
			throw new Exception("$Attempted to create NCreatureVisuals from path that doesn't exist {path}");
		}
		return NodeFactory<NCreatureVisuals>.CreateFromResource(PreloadManager.Cache.GetTexture2D(path));
	}

	[Obsolete("Use NodeFactory<NCreatureVisuals>.CreateFromScene instead.")]
	public static NCreatureVisuals CreatureVisualsFromScene(string path)
	{
		return NodeFactory<NCreatureVisuals>.CreateFromScene(path);
	}

	public static T TransferAllNodes<T>(this T obj, string sourceScene, params string[] uniqueNames) where T : Node
	{
		Node val = PreloadManager.Cache.GetScene(sourceScene).Instantiate((GenEditState)0);
		List<string> list = TransferNodes((Node)(object)obj, val, uniqueNames);
		if (list.Count > 0)
		{
			BaseLibMain.Logger.Warn("Created " + ((object)val).GetType().FullName + " missing required children " + string.Join(" ", list), 1);
		}
		return obj;
	}

	private static List<string> TransferNodes(Node target, Node source, params string[] names)
	{
		return TransferNodes(target, source, uniqueNames: true, names);
	}

	private static List<string> TransferNodes(Node target, Node source, bool uniqueNames, params string[] names)
	{
		target.Name = source.Name;
		List<string> list = names.ToList();
		foreach (Node child in source.GetChildren(false))
		{
			source.RemoveChild(child);
			if (list.Remove(StringName.op_Implicit(child.Name)) && uniqueNames)
			{
				child.UniqueNameInOwner = true;
			}
			target.AddChild(child, false, (InternalMode)0);
			child.Owner = target;
			SetChildrenOwner(target, child);
		}
		source.QueueFree();
		return list;
	}

	private static void SetChildrenOwner(Node target, Node child)
	{
		foreach (Node child2 in child.GetChildren(false))
		{
			child2.Owner = target;
			SetChildrenOwner(target, child2);
		}
	}
}
