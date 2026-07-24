using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigSliderAttribute(double min = 0.0, double max = 100.0, double step = 1.0) : Attribute()
{
	public double Min { get; } = min;

	public double Max { get; } = max;

	public double Step { get; } = step;

	public string? Format { get; set; }
}
