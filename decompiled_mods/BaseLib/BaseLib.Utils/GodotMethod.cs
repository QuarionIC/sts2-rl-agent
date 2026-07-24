using Godot;

namespace BaseLib.Utils;

public class GodotMethod(StringName name)
{
	public Variant Invoke(GodotObject obj, params Variant[] args)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return obj.Call(name, args);
	}

	public static implicit operator GodotMethodDelegate(GodotMethod godotMethod)
	{
		return godotMethod.AsDelegate();
	}

	public GodotMethodDelegate AsDelegate()
	{
		return Invoke;
	}
}
