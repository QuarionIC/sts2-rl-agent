using System.Collections.Generic;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Powers;
using Downfall.DownfallCode.Localization;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Localization;

public class ChantDescriptionSource : IExtraDescriptionSource
{
	public IEnumerable<string> GetLines(CardModel card)
	{
		if (card is IChantable chantable)
		{
			string text = ((AbstractModel)card).Id.Entry + ".chant";
			bool hasChanted = chantable.HasChanted;
			LocString val = new LocString("chants", text);
			val.Add("Chanted", hasChanted);
			card.DynamicVars.AddTo(val);
			LocString val2 = new LocString("card_keywords", "AWAKENED-CHANT.card_text");
			val2.Add("Chanted", hasChanted);
			bool num = ((AbstractModel)card).IsCanonical || card._owner == null || AwakenedCmd.WasLastCardPlayedPower(card) || hasChanted;
			string formattedText = new LocString("card_keywords", "COLON").GetFormattedText();
			string value = (hasChanted ? ("[img]" + ModelDb.Power<ChosenVersePower>().CustomPackedSpritePath + "[/img] ") : "");
			string value2 = (num ? val.GetFormattedText() : ("[color=#FFFFFF88]" + val.GetFormattedText() + "[/color]"));
			yield return $"{value}{val2.GetFormattedText()}{formattedText}\n{value2}";
		}
	}
}
