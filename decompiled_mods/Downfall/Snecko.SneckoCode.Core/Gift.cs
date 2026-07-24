using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Snecko.SneckoCode.Core;

public readonly struct Gift
{
	public CardRarity? Rarity { get; init; }

	public CardType? Type { get; init; }

	public bool IsDebuff { get; init; }

	public bool IsStrike { get; init; }

	public int? MinCost { get; init; }

	public int? Gold { get; init; }

	public bool IsUpgraded { get; init; }

	public bool Matches(CardModel card)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (Rarity.HasValue && card.Rarity != Rarity.Value)
		{
			return false;
		}
		if (Type.HasValue && card.Type != Type.Value)
		{
			return false;
		}
		if (IsDebuff && !SneckoCmd.IsDebuff(card))
		{
			return false;
		}
		if (IsStrike && !card.Tags.Contains((CardTag)1))
		{
			return false;
		}
		if (MinCost.HasValue)
		{
			return card.EnergyCost.Canonical >= MinCost.Value;
		}
		return true;
	}
}
