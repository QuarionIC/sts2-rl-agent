using System.Collections.Generic;
using Downfall.DownfallCode.Localization;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Localization;

public class GemDescriptionSource : IExtraDescriptionSource
{
	private static LocString EmptyGemDescription => new LocString("gems", "Guardian".ToUpperInvariant() + "-EMPTY_SLOT.description");

	public IEnumerable<string> GetLines(CardModel card)
	{
		if (!(card is IGemSocketCard gc))
		{
			yield break;
		}
		for (int i = 0; i < gc.GemSlots; i++)
		{
			if (i < gc.Gems.Count)
			{
				string text = gc.Gems[i].GetFormattedText(cardText: true);
				if (text.Equals(""))
				{
					text = "-";
				}
				yield return "❮ " + text + " ❯";
			}
			else
			{
				yield return EmptyGemDescription.GetFormattedText();
			}
		}
		if (card is IGemCard gemCard)
		{
			if (((AbstractModel)card).IsMutable)
			{
				yield return gemCard.GemModel.GetFormattedText();
			}
			else
			{
				yield return gemCard.CanonicalGemModel.GetFormattedText();
			}
		}
	}
}
