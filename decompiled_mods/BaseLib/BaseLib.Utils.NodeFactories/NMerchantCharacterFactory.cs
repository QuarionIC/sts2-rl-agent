using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BaseLib.Utils.NodeFactories;

internal class NMerchantCharacterFactory : NodeFactory<NMerchantCharacter>
{
	internal NMerchantCharacterFactory()
		: base((IEnumerable<INodeInfo>)Array.Empty<INodeInfo>())
	{
	}

	protected override NMerchantCharacter CreateBareFromResource(object resource)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		Texture2D val = (Texture2D)((resource is Texture2D) ? resource : null);
		if (val != null)
		{
			BaseLibMain.Logger.Info("Creating NMerchantCharacterFactory from Texture2D", 1);
			Vector2 size = val.GetSize();
			NMerchantCharacter val2 = new NMerchantCharacter();
			Sprite2D val3 = new Sprite2D();
			((Node)val2).AddUnique((Node)(object)val3, "Visuals");
			val3.Texture = val;
			((Node2D)val3).Position = new Vector2(0f, (0f - size.Y) * 0.5f);
			return val2;
		}
		return base.CreateBareFromResource(resource);
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
	}
}
