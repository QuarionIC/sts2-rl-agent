using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigColorPickerAttribute : Attribute
{
	public bool EditAlpha { get; set; } = true;

	public bool EditIntensity { get; set; }
}
