using System.Linq;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class MaintenanceStrikePower : HermitPowerModel, IModifyDamageAdditive
{
	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return (dealer == ((PowerModel)this).Owner && cardSource != null && (int)cardSource.Rarity == 1 && cardSource.Tags.Contains((CardTag)1) && ValuePropExtensions.IsPoweredAttack(props)) ? ((PowerModel)this).Amount : 0;
	}

	public MaintenanceStrikePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
