using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class ConfigVisibleIfAttribute(string targetName, params object?[] args) : Attribute()
{
	public string TargetName { get; } = targetName;

	public object?[] Args { get; } = args;

	public bool Invert { get; set; }
}
