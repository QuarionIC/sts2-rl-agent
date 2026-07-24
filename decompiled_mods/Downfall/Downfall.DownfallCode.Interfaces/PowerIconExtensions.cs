using Godot;

namespace Downfall.DownfallCode.Interfaces;

public static class PowerIconExtensions
{
	public static void AddDecoration(this TextureRect icon, Control node, int index)
	{
		((Node)node).Name = StringName.op_Implicit($"_custom_icon_{index}");
		((Node)icon).AddChild((Node)(object)node, false, (InternalMode)0);
	}
}
