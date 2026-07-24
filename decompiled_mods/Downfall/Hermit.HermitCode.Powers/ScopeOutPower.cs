using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public class ScopeOutPower : HermitPowerModel
{
	public ScopeOutPower()
		: base((PowerType)1, (PowerStackType)2)
	{
	}

	public override Task BeforeAttack(AttackCommand command)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (command.Attacker == ((PowerModel)this).Owner)
		{
			AbstractModel modelSource = command.ModelSource;
			CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
			if (val != null && val.Tags.Contains((CardTag)1) && (int)val.Rarity == 1)
			{
				command._singleTarget = null;
				command._combatState = ((PowerModel)this).CombatState;
				command.IsRandomlyTargeted = false;
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}
}
