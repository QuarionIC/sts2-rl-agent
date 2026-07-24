using System;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;

namespace Downfall.DownfallCode.Localization;

public class PlusIfUpgradedFormatter : IFormatter
{
	public string Name
	{
		get
		{
			return "plus";
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public bool CanAutoDetect { get; set; }

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		object currentValue = formattingInfo.CurrentValue;
		IfUpgradedVar val = (IfUpgradedVar)((currentValue is IfUpgradedVar) ? currentValue : null);
		if (val == null)
		{
			return false;
		}
		UpgradeDisplay upgradeDisplay = val.upgradeDisplay;
		switch ((int)upgradeDisplay)
		{
		case 1:
			formattingInfo.Write("+");
			break;
		case 2:
			formattingInfo.Write("[green]+[/green]");
			break;
		default:
			throw new ArgumentOutOfRangeException("formattingInfo", $"Unexpected value: {val.upgradeDisplay}");
		case 0:
			break;
		}
		return true;
	}
}
