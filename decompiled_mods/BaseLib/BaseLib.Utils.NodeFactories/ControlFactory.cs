using System;
using System.Collections.Generic;
using Godot;

namespace BaseLib.Utils.NodeFactories;

internal class ControlFactory : NodeFactory<Control>
{
	internal ControlFactory()
		: base((IEnumerable<INodeInfo>)Array.Empty<INodeInfo>())
	{
	}

	protected override Control CreateBareFromResource(object resource)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		Texture2D val = (Texture2D)((resource is Texture2D) ? resource : null);
		if (val != null)
		{
			Vector2 size = val.GetSize();
			return (Control)new TextureRect
			{
				Name = StringName.op_Implicit(((Resource)val).ResourcePath),
				Size = size,
				Texture = val,
				PivotOffset = size / 2f,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5,
				MouseFilter = (MouseFilterEnum)2
			};
		}
		return base.CreateBareFromResource(resource);
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
	}
}
