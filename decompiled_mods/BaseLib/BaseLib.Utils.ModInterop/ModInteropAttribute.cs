using System;

namespace BaseLib.Utils.ModInterop;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModInteropAttribute(string modId, string? type = null) : Attribute()
{
	public string ModId { get; } = modId;

	public string? Type { get; } = type;
}
