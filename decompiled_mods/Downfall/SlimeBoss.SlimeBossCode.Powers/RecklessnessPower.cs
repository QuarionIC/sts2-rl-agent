using System.Linq;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Powers;

public class RecklessnessPower : SlimeBossPowerModel, IModifyDamageAdditive
{
	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return (dealer == ((PowerModel)this).Owner && cardSource != null && cardSource.Tags.Contains(SlimeBossTag.Tackle) && target == dealer) ? ((PowerModel)this).Amount : 0;
	}

	public RecklessnessPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
