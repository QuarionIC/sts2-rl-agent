using System;

namespace BaseLib.Patches.Content;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class CustomEnumAttribute(string? name = null) : Attribute()
{
	public string? Name { get; } = name;
}
