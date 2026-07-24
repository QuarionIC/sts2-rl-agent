using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigDropdownOverrideLocalizationAttribute(string overridePropertyName) : Attribute()
{
	public string OverridePropertyName { get; } = overridePropertyName;
}
