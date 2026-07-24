using System.Globalization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;

namespace Downfall.DownfallCode.Localization;

public class PreviewValueFormatter : IFormatter
{
	public string Name { get; set; } = "preview";

	public bool CanAutoDetect { get; set; }

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		object currentValue = formattingInfo.CurrentValue;
		DynamicVar val = (DynamicVar)((currentValue is DynamicVar) ? currentValue : null);
		if (val == null)
		{
			return false;
		}
		formattingInfo.Write(val.PreviewValue.ToString(CultureInfo.InvariantCulture));
		return true;
	}
}
