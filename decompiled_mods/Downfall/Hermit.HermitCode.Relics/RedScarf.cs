using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Relics;

public sealed class RedScarf : HermitRelicModel
{
	public RedScarf()
		: base((RelicRarity)4)
	{
		WithBlock(3);
	}

	public override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
		if (amount != 0m && target.IsEnemy && (int)power.GetTypeForAmount(amount) == 2)
		{
			PowerModel power2 = target.GetPower(((AbstractModel)power).Id);
			if ((power2 == null || power2.Amount == 0) && applier == ((RelicModel)this).Owner.Creature)
			{
				((RelicModel)this).Flash();
				await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars.Block, (CardPlay)null, false);
			}
		}
	}
}
