using System.Linq;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class HoneBladePower : ChampPowerModel, IModifyDamageAdditive
{
	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (ValuePropExtensions.IsPoweredAttack(props) && cardSource != null && cardSource.Tags.Contains((CardTag)1) && (dealer == ((PowerModel)this).Owner || cardSource.Owner.Creature == ((PowerModel)this).Owner))
		{
			return ((PowerModel)this).Amount;
		}
		return 0m;
	}

	public HoneBladePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
