using System;
using System.Collections.Generic;
using System.Globalization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using SmartFormat.Utilities;

namespace Downfall.DownfallCode.Localization;

public class PreviewPluralFormatter : IFormatter
{
	public string Name { get; set; } = "pplural";

	public bool CanAutoDetect { get; set; }

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		object currentValue = formattingInfo.CurrentValue;
		DynamicVar val = (DynamicVar)((currentValue is DynamicVar) ? currentValue : null);
		if (val == null)
		{
			return false;
		}
		Format format = formattingInfo.Format;
		IList<Format> list = ((format != null) ? format.Split('|') : null);
		if (list == null || list.Count < 2)
		{
			return false;
		}
		int index = PluralRules.GetPluralRule(GetCultureInfo(formattingInfo).TwoLetterISOLanguageName).Invoke(val.PreviewValue, list.Count);
		formattingInfo.FormatAsChild(list[index], formattingInfo.CurrentValue);
		return true;
	}

	private static CultureInfo GetCultureInfo(IFormattingInfo formattingInfo)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		string text = formattingInfo.FormatterOptions.Trim();
		CultureInfo cultureInfo;
		if (text == string.Empty)
		{
			cultureInfo = ((!(formattingInfo.FormatDetails.Provider is CultureInfo cultureInfo2)) ? CultureInfo.CurrentUICulture : cultureInfo2);
			if (cultureInfo.Equals(CultureInfo.InvariantCulture))
			{
				cultureInfo = CultureInfo.GetCultureInfo("en");
			}
		}
		else
		{
			try
			{
				cultureInfo = CultureInfo.GetCultureInfo(text);
			}
			catch (Exception ex)
			{
				throw new FormattingException((FormatItem)(object)formattingInfo.Format, ex, 0);
			}
		}
		return cultureInfo;
	}
}
