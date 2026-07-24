using System.Collections.Generic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Interfaces;
using Downfall.DownfallCode.Localization;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Localization;

public class EncodeDescriptionSource : IExtraDescriptionSource
{
	public IEnumerable<string> GetLines(CardModel card)
	{
		if (AutomatonCmd.IsEncodable(card) && card is IEncodable encodable)
		{
			string text = encodable.EncodeString(card);
			string formattedText = new LocString("static_hover_tips", "AUTOMATON-ENCODE.title").GetFormattedText();
			string formattedText2 = new LocString("card_keywords", "PERIOD").GetFormattedText();
			string text2 = "[gold]" + formattedText + "[/gold]" + formattedText2;
			yield return string.IsNullOrEmpty(text) ? text2 : (text + "\n" + text2);
		}
	}
}
