using System;
using BaseLib.Config.UI;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Method)]
public class ConfigButtonAttribute(string buttonLabelKey) : Attribute()
{
	public string ButtonLabelKey { get; } = buttonLabelKey;

	public string Color { get; set; } = NConfigButton.DefaultColor;
}
