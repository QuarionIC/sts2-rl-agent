using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BaseLib.Abstracts;

public abstract class CustomRestSiteOption : RestSiteOption
{
	public virtual string? CustomIconPath => null;

	protected CustomRestSiteOption(Player owner)
		: base(owner)
	{
	}
}
