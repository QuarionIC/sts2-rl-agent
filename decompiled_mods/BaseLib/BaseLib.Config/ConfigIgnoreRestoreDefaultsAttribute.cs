using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigIgnoreRestoreDefaultsAttribute : Attribute
{
}
