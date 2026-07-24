using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BaseLib.Patches.Fixes;

internal sealed class RewardExtData
{
	[JsonPropertyName("flags")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Flags { get; set; }

	[JsonPropertyName("custom_card_ids")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? CustomCardIds { get; set; }

	[JsonPropertyName("is_custom_pool")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool IsCustomPool { get; set; }

	[JsonPropertyName("source")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Source { get; set; }

	[JsonPropertyName("rarity_odds")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int RarityOdds { get; set; }
}
