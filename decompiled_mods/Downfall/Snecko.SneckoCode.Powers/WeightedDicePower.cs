using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class WeightedDicePower : SneckoPowerModel
{
	public WeightedDicePower()
		: base((PowerType)1, (PowerStackType)2)
	{
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != ((PowerModel)this).Owner || card.EnergyCost.CostsX || card.EnergyCost.GetResolved() <= card.EnergyCost.GetWithModifiers((CostModifiers)0))
		{
			return playCount;
		}
		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		((PowerModel)this).Flash();
		return Task.CompletedTask;
	}
}
