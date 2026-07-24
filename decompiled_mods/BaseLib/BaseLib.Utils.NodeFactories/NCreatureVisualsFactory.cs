using System.Collections.Generic;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Utils.NodeFactories;

internal class NCreatureVisualsFactory : NodeFactory<NCreatureVisuals>
{
	internal NCreatureVisualsFactory()
		: base((IEnumerable<INodeInfo>)new _003C_003Ez__ReadOnlyArray<INodeInfo>(new INodeInfo[7]
		{
			new NodeInfo<Node2D>("%Visuals"),
			new NodeInfo<Node2D>("%PhobiaModeVisuals"),
			new NodeInfo<Control>("Bounds"),
			new NodeInfo<Marker2D>("%CenterPos"),
			new NodeInfo<Marker2D>("IntentPos"),
			new NodeInfo<Marker2D>("%OrbPos"),
			new NodeInfo<Marker2D>("%TalkPos")
		}))
	{
	}

	protected override NCreatureVisuals CreateBareFromResource(object resource)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		Texture2D val = (Texture2D)((resource is Texture2D) ? resource : null);
		if (val != null)
		{
			BaseLibMain.Logger.Info("Creating NCreatureVisuals from Texture2D", 1);
			Vector2 size = val.GetSize();
			Vector2 val2 = val.GetSize() * 1.1f;
			NCreatureVisuals val3 = new NCreatureVisuals();
			Control val4 = new Control();
			((Node)val3).AddUnique((Node)(object)val4, "Bounds");
			val4.Position = new Vector2((0f - val2.X) / 2f, 0f - val2.Y);
			val4.Size = val2;
			Sprite2D val5 = new Sprite2D();
			((Node)val3).AddUnique((Node)(object)val5, "Visuals");
			val5.Texture = val;
			((Node2D)val5).Position = new Vector2(0f, (0f - size.Y) * 0.5f);
			return val3;
		}
		return base.CreateBareFromResource(resource);
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		switch (required.Path)
		{
		case "Bounds":
		{
			Control node = new Control();
			node.Size = new Vector2(240f, 280f);
			node.Position = new Vector2(-120f, -280f);
			target.AddUnique((Node)(object)node, "Bounds");
			break;
		}
		case "%Visuals":
			BaseLibMain.Logger.Warn("'Visuals' node must be provided for NCreatureVisuals", 1);
			break;
		case "IntentPos":
		{
			Control node = target.GetNode<Control>(NodePath.op_Implicit("%Bounds"));
			Marker2D val2 = new Marker2D();
			target.AddUnique((Node)(object)val2, "IntentPos");
			((Node2D)val2).Position = node.Position + node.Size * new Vector2(0.5f, 0f) + new Vector2(0f, -70f);
			break;
		}
		case "%CenterPos":
		{
			Control node = target.GetNode<Control>(NodePath.op_Implicit("%Bounds"));
			Marker2D val = new Marker2D();
			target.AddUnique((Node)(object)val, "CenterPos");
			((Node2D)val).Position = node.Position + node.Size * new Vector2(0.5f, 0.6f);
			break;
		}
		}
	}
}
