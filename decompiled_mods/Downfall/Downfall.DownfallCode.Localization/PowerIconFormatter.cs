using System;
using Downfall.DownfallCode.Abstract;
using SmartFormat.Core.Extensions;

namespace Downfall.DownfallCode.Localization;

public class PowerIconFormatter : IFormatter
{
	public string Name
	{
		get
		{
			return "icon";
		}
		set
		{
			throw new Exception();
		}
	}

	public bool CanAutoDetect { get; set; }

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		if (!(formattingInfo.CurrentValue is DownfallPowerModel downfallPowerModel))
		{
			return false;
		}
		formattingInfo.Write("[img]" + downfallPowerModel.CustomPackedSpritePath + "[/img]");
		return true;
	}
}
