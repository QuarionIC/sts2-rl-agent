using System;

namespace BaseLib.Patches.Content;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class KeywordPropertiesAttribute : Attribute
{
	public AutoKeywordPosition Position { get; }

	public bool RichKeyword { get; }

	public KeywordPropertiesAttribute(AutoKeywordPosition position)
		: this(position, richKeyword: true)
	{
	}

	public KeywordPropertiesAttribute(AutoKeywordPosition position, bool richKeyword)
	{
		Position = position;
		RichKeyword = richKeyword;
	}
}
