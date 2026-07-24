using MegaCrit.Sts2.Core.Models;

namespace Act4Heart.Powers;

internal abstract class A4hPowerModel : PowerModel
{
	internal string icon_path => "res://Act4Heart/images/powers/" + ((AbstractModel)this).Id.Entry.ToLowerInvariant() + ".png";
}
