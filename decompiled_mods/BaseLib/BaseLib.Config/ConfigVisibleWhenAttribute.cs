using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
[Obsolete("No longer functional. Use ConfigVisibleIfAttribute instead.", true)]
public class ConfigVisibleWhenAttribute(string watchedPropertyName, object expectedValue, bool invert = false) : Attribute()
{
	public string WatchedPropertyName { get; } = watchedPropertyName;

	public object ExpectedValue { get; } = expectedValue;

	public bool Invert { get; } = invert;
}
