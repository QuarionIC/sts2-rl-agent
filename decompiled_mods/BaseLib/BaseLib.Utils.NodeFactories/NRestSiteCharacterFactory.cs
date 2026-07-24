using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace BaseLib.Utils.NodeFactories;

internal class NRestSiteCharacterFactory : NodeFactory<NRestSiteCharacter>
{
	public NRestSiteCharacterFactory()
		: base((IEnumerable<INodeInfo>)new _003C_003Ez__ReadOnlyArray<INodeInfo>(new INodeInfo[5]
		{
			new NodeInfo<Control>("ControlRoot", MakeNameUnique: false),
			new NodeInfo<Control>("%Hitbox"),
			new NodeInfo<NSelectionReticle>("%SelectionReticle"),
			new NodeInfo<Control>("%ThoughtBubbleRight"),
			new NodeInfo<Control>("%ThoughtBubbleLeft")
		}))
	{
	}

	protected override NRestSiteCharacter CreateBareFromResource(object resource)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		Texture2D val = (Texture2D)((resource is Texture2D) ? resource : null);
		if (val != null)
		{
			BaseLibMain.Logger.Info("Creating NRestSiteCharacter from Texture2D", 1);
			Vector2 size = val.GetSize();
			Vector2 val2 = val.GetSize() * 1.05f;
			NRestSiteCharacter val3 = new NRestSiteCharacter
			{
				Name = StringName.op_Implicit("GeneratedRestSiteChar_" + StringExtensions.GetFile(((Resource)val).ResourcePath))
			};
			Control val4 = new Control();
			((Node)val4).Name = StringName.op_Implicit("ControlRoot");
			((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
			val4.Position = Vector2.Zero;
			val4.Size = Vector2.Zero;
			Control val5 = new Control();
			((Node)(object)val4).AddUnique((Node)(object)val5, "Hitbox");
			val5.Position = new Vector2((0f - val2.X) * 0.5f, (0f - val2.Y) * 0.6f);
			val5.Size = val2;
			Sprite2D val6 = new Sprite2D();
			((Node)val6).Name = StringName.op_Implicit("Visuals");
			((Node)val4).AddChild((Node)(object)val6, false, (InternalMode)0);
			val6.Texture = val;
			((Node2D)val6).Position = new Vector2(0f, (0f - size.Y) * 0.1f);
			return val3;
		}
		return base.CreateBareFromResource(resource);
	}

	protected override void ConvertScene(NRestSiteCharacter target, Node? source)
	{
		Sprite2D val = (Sprite2D)(object)((source is Sprite2D) ? source : null);
		if (val != null)
		{
			Texture2D texture = val.Texture;
			if (texture != null)
			{
				GodotTreeExtensions.QueueFreeSafely(source);
				source = (Node?)(object)CreateBareFromResource(texture);
			}
		}
		base.ConvertScene(target, source);
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		switch (required.Path)
		{
		case "ControlRoot":
		case "%Hitbox":
			BaseLibMain.Logger.Warn(required.Path + " must be defined in NRestSiteCharacter scene.", 1);
			break;
		case "%ThoughtBubbleRight":
		{
			Control node = target.GetNode<Control>(NodePath.op_Implicit("%Hitbox"));
			Control val3 = new Control();
			val3.Size = Vector2.Zero;
			val3.Position = node.Position + node.Size * new Vector2(0.8f, 0.2f);
			target.AddUnique((Node)(object)val3, "ThoughtBubbleRight");
			break;
		}
		case "%ThoughtBubbleLeft":
		{
			Control node = target.GetNode<Control>(NodePath.op_Implicit("%Hitbox"));
			Control val2 = new Control();
			val2.Size = Vector2.Zero;
			val2.Position = node.Position + node.Size * new Vector2(0.2f, 0.2f);
			target.AddUnique((Node)(object)val2, "ThoughtBubbleLeft");
			break;
		}
		case "%SelectionReticle":
		{
			Control node = target.GetNode<Control>(NodePath.op_Implicit("%Hitbox"));
			NSelectionReticle val = SceneHelper.Instantiate<NSelectionReticle>("ui/selection_reticle");
			NodeFactory.CopyControlProperties((Control)(object)val, node);
			target.AddUnique((Node)(object)val, "SelectionReticle");
			break;
		}
		}
	}

	protected override Node ConvertNodeType(Node node, Type targetType)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		if (targetType == typeof(NSelectionReticle))
		{
			Control val = (Control)(object)((node is Control) ? node : null);
			if (val == null)
			{
				return base.ConvertNodeType(node, targetType);
			}
			NSelectionReticle val2 = SceneHelper.Instantiate<NSelectionReticle>("ui/selection_reticle");
			((Node)val2).Name = ((Node)val).Name;
			NodeFactory.CopyControlProperties((Control)(object)val2, val);
			((Node)val).ReplaceBy((Node)(object)val2, false);
			((Node)val).QueueFree();
			return (Node)(object)val2;
		}
		if (targetType == typeof(Control))
		{
			Marker2D val3 = (Marker2D)(object)((node is Marker2D) ? node : null);
			if (val3 != null)
			{
				if (((Node)val3).Name.Equals(StringName.op_Implicit("ThoughtBubbleLeft")) || ((Node)val3).Name.Equals(StringName.op_Implicit("ThoughtBubbleRight")) || ((Node)val3).Name.Equals(StringName.op_Implicit("ControlRoot")))
				{
					Control val4 = new Control
					{
						Name = ((Node)val3).Name,
						Size = Vector2.Zero,
						Position = ((Node2D)val3).Position
					};
					((Node)val3).ReplaceBy((Node)(object)val4, false);
					((Node)val3).QueueFree();
					return (Node)(object)val4;
				}
				throw new InvalidOperationException($"Marker2D can only be converted to Control for 'ControlRoot', 'ThoughtBubbleLeft', and 'ThoughtBubbleRight' in NRestSiteCharacter, not for '{((Node)val3).Name}'");
			}
		}
		return base.ConvertNodeType(node, targetType);
	}
}
