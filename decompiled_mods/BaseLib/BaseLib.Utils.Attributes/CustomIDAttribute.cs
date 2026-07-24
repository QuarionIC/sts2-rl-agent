using System;

namespace BaseLib.Utils.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CustomIDAttribute(string id) : Attribute()
{
	public string ID { get; } = id;
}
