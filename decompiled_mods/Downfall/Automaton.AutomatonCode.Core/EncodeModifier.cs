using System.Collections.Generic;
using System.Linq;
using Automaton.AutomatonCode.Cards.Token;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Core;

public abstract class EncodeModifier : DownfallCardModifier
{
	public string Identifier => RemoveEncodeSuffix(((AbstractModel)this).Id.Entry);

	protected virtual LocString EncodeLocString
	{
		get
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			LocString val = new LocString("encode", Identifier + ".encode");
			((CardModifier)this).DynamicVars.AddTo(val);
			return val;
		}
	}

	private static string RemoveEncodeSuffix(string input)
	{
		if (!input.EndsWith("_ENCODE"))
		{
			return input;
		}
		int length = "_ENCODE".Length;
		return input.Substring(0, input.Length - length);
	}

	public override void ModifyDescriptionPost(Creature? target, ref string description)
	{
		if (((CardModifier)this).Owner == null)
		{
			return;
		}
		foreach (KeyValuePair<string, DynamicVar> dynamicVar in ((CardModifier)this).DynamicVars)
		{
			dynamicVar.Value.UpdateCardPreview(((CardModifier)this).Owner, (CardPreviewMode)0, target, ((CardModifier)this).Owner.CombatState != null);
		}
		string formattedText = EncodeLocString.GetFormattedText();
		string text = BuildEncodeLine(formattedText);
		description = JoinNonEmpty("\n", description, text);
	}

	private string BuildEncodeLine(string text)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (((CardModifier)this).Owner is FunctionCard)
		{
			return text;
		}
		string formattedText = new LocString("static_hover_tips", "AUTOMATON-ENCODE.title").GetFormattedText();
		string formattedText2 = new LocString("card_keywords", "PERIOD").GetFormattedText();
		string text2 = "[gold]" + formattedText + "[/gold]" + formattedText2;
		return JoinNonEmpty("\n", text2, text);
	}

	private static string JoinNonEmpty(string separator, params string?[] parts)
	{
		return string.Join(separator, parts.Where((string p) => !string.IsNullOrEmpty(p)));
	}

	public static EncodeModifier? On(CardModel card)
	{
		return CardModifier.Modifiers(card).OfType<EncodeModifier>().FirstOrDefault();
	}
}
