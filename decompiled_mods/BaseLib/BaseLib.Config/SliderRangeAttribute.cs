using System;

namespace BaseLib.Config;

[Obsolete("Use [ConfigSlider] instead. This will be removed in future versions.")]
[AttributeUsage(AttributeTargets.Property)]
public class SliderRangeAttribute : ConfigSliderAttribute
{
	public SliderRangeAttribute(double min, double max, double step = 1.0)
		: base(min, max, step)
	{
	}
}
