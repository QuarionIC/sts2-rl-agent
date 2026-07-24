using System;
using Godot;

namespace BaseLib.Utils;

public class AddedNode<TParentType, TNode> : ReadonlySpireField<TParentType, TNode>, IAddedNodes<TParentType> where TParentType : Node where TNode : Node
{
	public AddedNode(Func<TParentType, TNode> defaultVal)
		: base(defaultVal)
	{
		IAddedNodes<TParentType>._addedNodes.Add(this);
		IAddedNodes<TParentType>.PatchNodeReady();
	}

	public AddedNode(string scenePath, Action<TParentType, TNode>? extraSetup = null)
		: this((Func<TParentType, TNode>)delegate(TParentType parent)
		{
			TNode val = ResourceLoader.Load<PackedScene>(scenePath, (string)null, (CacheMode)1).Instantiate<TNode>((GenEditState)0);
			extraSetup?.Invoke(parent, val);
			return val;
		})
	{
	}

	public Node? GetNode(TParentType obj)
	{
		return (Node?)(object)Get(obj);
	}
}
