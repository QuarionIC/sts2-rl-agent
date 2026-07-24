using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomOrbModel : OrbModel, ICustomModel, ILocalizationProvider
{
	internal static readonly List<CustomOrbModel> RegisteredOrbs = new List<CustomOrbModel>();

	public virtual string? CustomIconPath => null;

	public virtual string? CustomSpritePath => null;

	public virtual bool IncludeInRandomPool => false;

	public virtual string? CustomPassiveSfx => null;

	public virtual string? CustomEvokeSfx => null;

	public virtual string? CustomChannelSfx => null;

	protected override string PassiveSfx => CustomPassiveSfx ?? ((OrbModel)this).PassiveSfx;

	protected override string EvokeSfx => CustomEvokeSfx ?? ((OrbModel)this).EvokeSfx;

	protected override string ChannelSfx => CustomChannelSfx ?? ((OrbModel)this).ChannelSfx;

	public virtual List<(string, string)>? Localization => null;

	public virtual Node2D? CreateCustomSprite()
	{
		return null;
	}

	public CustomOrbModel()
	{
		RegisteredOrbs.Add(this);
	}
}
