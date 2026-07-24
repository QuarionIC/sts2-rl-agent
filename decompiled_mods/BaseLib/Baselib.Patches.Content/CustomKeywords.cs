using System.Collections.Generic;

namespace BaseLib.Patches.Content;

public static class CustomKeywords
{
	public readonly struct KeywordInfo
	{
		public readonly string Key;

		public required AutoKeywordPosition AutoPosition { get; init; }

		public required bool RichKeyword { get; init; }

		public KeywordInfo(string key)
		{
			AutoPosition = AutoKeywordPosition.None;
			RichKeyword = false;
			Key = key;
		}

		public static implicit operator string(KeywordInfo info)
		{
			return info.Key;
		}
	}

	public static readonly Dictionary<int, KeywordInfo> KeywordIDs = new Dictionary<int, KeywordInfo>();
}
