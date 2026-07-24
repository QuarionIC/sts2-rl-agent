using System;

namespace BaseLib.Config;

[Obsolete("Use the Format property on [ConfigSlider] instead. This will be removed in future versions.")]
[AttributeUsage(AttributeTargets.Property)]
public class SliderLabelFormatAttribute(string format) : Attribute()
{
	public string Format { get; } = format;
}
