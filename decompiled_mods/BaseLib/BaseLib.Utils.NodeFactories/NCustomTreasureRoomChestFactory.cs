using System;
using System.Collections.Generic;
using BaseLib.BaseLibScenes.Acts;
using Godot;

namespace BaseLib.Utils.NodeFactories;

internal class NCustomTreasureRoomChestFactory : NodeFactory<NCustomTreasureRoomChest>
{
	public NCustomTreasureRoomChestFactory()
		: base((IEnumerable<INodeInfo>)Array.Empty<INodeInfo>())
	{
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
	}
}
