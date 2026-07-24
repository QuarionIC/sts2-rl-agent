using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class PowerArmor : ChampRelicModel
{
	private const int Cap = 10;

	public PowerArmor()
		: base((RelicRarity)5)
	{
		WithTip<StrengthPower>();
		WithTip<VigorPower>();
		WithTip<CounterPower>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				((RelicModel)this).Flash();
				await PowerCmd.Apply<StrengthPower>(ctx, ((RelicModel)this).Owner.Creature, 2m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}
	}

	public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		bool flag = target != ((RelicModel)this).Owner.Creature;
		if (!flag)
		{
			bool flag2 = ((power is VigorPower || power is CounterPower) ? true : false);
			flag = !flag2;
		}
		if (flag || amount <= 0m)
		{
			return 0m;
		}
		int num = 10 - power.Amount;
		return ((num <= 0) ? 0m : Math.Min(amount, num)) - amount;
	}
}
