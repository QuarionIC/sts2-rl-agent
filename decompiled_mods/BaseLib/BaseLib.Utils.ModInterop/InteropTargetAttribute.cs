using System;

namespace BaseLib.Utils.ModInterop;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class InteropTargetAttribute : Attribute
{
	public string? Type { get; }

	public string? Name { get; }

	public InteropTargetAttribute(string type, string? name = null)
	{
		Type = type;
		Name = name;
	}

	public InteropTargetAttribute(string? name = null)
	{
		Name = name;
	}
}
