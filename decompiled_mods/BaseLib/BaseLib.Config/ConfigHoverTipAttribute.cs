using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class ConfigHoverTipAttribute(bool enabled = true) : Attribute()
{
	public bool Enabled { get; } = enabled;
}
