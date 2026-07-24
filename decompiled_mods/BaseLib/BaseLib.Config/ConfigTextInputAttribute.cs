using System;

namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigTextInputAttribute : Attribute
{
	public string AllowedCharactersRegex { get; }

	public int MaxLength { get; set; }

	public ConfigTextInputAttribute()
		: this(TextInputPreset.Anything)
	{
	}

	public ConfigTextInputAttribute(TextInputPreset preset)
	{
		AllowedCharactersRegex = preset switch
		{
			TextInputPreset.Alphanumeric => "[a-zA-Z0-9]+", 
			TextInputPreset.AlphanumericWithSpaces => "[a-zA-Z0-9 ]+", 
			TextInputPreset.SafeDisplayName => "[\\p{L}\\d_\\- ]+", 
			_ => ".*", 
		};
	}

	public ConfigTextInputAttribute(string customRegex)
	{
		AllowedCharactersRegex = customRegex;
	}
}
