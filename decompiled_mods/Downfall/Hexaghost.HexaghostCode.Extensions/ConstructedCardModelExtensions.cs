using BaseLib.Abstracts;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Hexaghost.HexaghostCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithAfterlife(this ConstructedCardModel card)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected I4, but got Unknown
		card.WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)2,
			(CardKeyword)(int)HexaghostKeyword.Afterlife
		});
		return card;
	}
}
