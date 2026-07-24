using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BaseLib.Extensions;

public static class NodeExtensions
{
	public static void AddUnique(this Node n, Node child, string? name = null)
	{
		if (name != null)
		{
			child.Name = StringName.op_Implicit(name);
		}
		child.UniqueNameInOwner = true;
		n.AddChild(child, false, (InternalMode)0);
		child.Owner = n;
	}

	public static Control? FindFirstFocusable(this Node? node)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I8
		if (node == null)
		{
			return null;
		}
		Control val = (Control)(object)((node is Control) ? node : null);
		if (val != null && (long)val.FocusMode == 2)
		{
			return val;
		}
		return ((IEnumerable<Node>)node.GetChildren(false)).Select(FindFirstFocusable).OfType<Control>().FirstOrDefault();
	}
}
