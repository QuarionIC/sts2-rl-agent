using System;

namespace BaseLib.Config;

[Obsolete("Use [ConfigHoverTipsByDefault] instead. This will be removed in future versions.")]
[AttributeUsage(AttributeTargets.Class)]
public class HoverTipsByDefaultAttribute : ConfigHoverTipsByDefaultAttribute
{
}
