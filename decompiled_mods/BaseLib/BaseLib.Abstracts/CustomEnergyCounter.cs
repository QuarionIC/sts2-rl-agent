using System;
using Godot;

namespace BaseLib.Abstracts;

public readonly struct CustomEnergyCounter
{
	public readonly Color OutlineColor;

	public readonly Color BurstColor;

	public CustomEnergyCounter(Func<int, string> pathFunc, Color outlineColor, Color burstColor)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		_003CpathFunc_003EP = pathFunc;
		OutlineColor = outlineColor;
		BurstColor = burstColor;
	}

	public string LayerImagePath(int layer)
	{
		return _003CpathFunc_003EP(layer);
	}
}
