using System;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Powers;

public class RollThroughPower : SlimeBossPowerModel, IModifySelfDamage, IModifyDamageAdditive
{
	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (dealer != ((PowerModel)this).Owner || cardSource == null || !cardSource.Tags.Contains(SlimeBossTag.Tackle) || !((Enum)props).HasFlag((Enum)(object)(ValueProp)4))
		{
			return 0m;
		}
		return -amount;
	}

	public decimal ModifySelfDamage(decimal amount, AbstractModel model)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val == null || !val.Tags.Contains(SlimeBossTag.Tackle) || val.Owner.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return 0m;
	}

	public Task AfterModifyingSelfDamage(AbstractModel model)
	{
		return PowerCmd.Decrement((PowerModel)(object)this);
	}

	public RollThroughPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
