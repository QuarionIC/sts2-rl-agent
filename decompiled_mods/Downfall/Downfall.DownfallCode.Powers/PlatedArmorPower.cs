using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Powers;

public class PlatedArmorPower : DownfallPowerModel
{
	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && result.UnblockedDamage != 0 && ValuePropExtensions.IsPoweredAttack(props))
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		Player player = ((PowerModel)this).Owner.Player;
		if (player == null || player.PlayerCombatState == null || side == ((PowerModel)this).Owner.Side)
		{
			((PowerModel)this).Flash();
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
		}
	}

	public PlatedArmorPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
