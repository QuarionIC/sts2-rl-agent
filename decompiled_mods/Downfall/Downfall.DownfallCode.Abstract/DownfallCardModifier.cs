using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public class DownfallCardModifier : CardModifier, ICustomModel
{
	public virtual bool ShouldGlowGold => false;

	protected LocString Description => new LocString("card_modifiers", ((AbstractModel)this).Id.Entry + ".description");
}
