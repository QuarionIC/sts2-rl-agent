using System;
using System.Collections.Generic;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.CustomEnums;

public readonly struct DownfallTip
{
	public static readonly DownfallTip Scry = new DownfallTip("Scry");

	private readonly string _name;

	private DownfallTip(string name)
	{
		_name = name;
	}

	public IHoverTip ToHoverTip()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_0065: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		string text = "DOWNFALL-" + _name.ToUpperInvariant();
		return (IHoverTip)(object)new HoverTip(new LocString("static_hover_tips", text + ".title"), LocManager.Instance.SmartFormat(new LocString("static_hover_tips", text + ".description"), new Dictionary<string, object> { ["energyPrefix"] = "" }), (Texture2D)null);
	}

	public static implicit operator TooltipSource(DownfallTip tip)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		return new TooltipSource((Func<CardModel, IHoverTip>)((CardModel _) => tip.ToHoverTip()));
	}
}
