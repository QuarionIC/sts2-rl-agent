using System;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions;

public static class CardModelExtensions
{
	public static CardModel CreateEcho(this CardModel card)
	{
		return card.CreateClone().ToEcho();
	}

	public static CardModel ToEcho(this CardModel card)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (card.IsEcho())
		{
			throw new InvalidOperationException($"Card {((AbstractModel)card).Id} is already an Echo.");
		}
		card.AddKeyword((CardKeyword)1);
		card.AddKeyword((CardKeyword)2);
		card.AddKeyword(DownfallKeyword.Echo);
		return card;
	}

	public static bool IsEcho(this CardModel card)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return card.Keywords.Contains(DownfallKeyword.Echo);
	}
}
