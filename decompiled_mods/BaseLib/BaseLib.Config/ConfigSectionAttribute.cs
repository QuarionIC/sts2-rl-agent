using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class ConfigSectionAttribute(string name) : Attribute()
{
	public string Name { get; } = name;
}
