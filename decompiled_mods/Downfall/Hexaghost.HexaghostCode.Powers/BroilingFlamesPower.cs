using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class BroilingFlamesPower : HexaghostPowerModel
{
	public override async Task AfterAttack(PlayerChoiceContext ctx, AttackCommand command)
	{
		if (!command.Results.SelectMany((List<DamageResult> r) => r).All((DamageResult e) => e.Receiver != ((PowerModel)this).Owner))
		{
			await PowerCmd.Apply<SoulBurnPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, command.Attacker, (CardModel)null, false);
		}
	}

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			return PowerCmd.Remove((PowerModel)(object)this);
		}
		return Task.CompletedTask;
	}

	public BroilingFlamesPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
