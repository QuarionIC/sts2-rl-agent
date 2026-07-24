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

namespace Downfall.DownfallCode.Powers;

public class StunnedPower : DownfallPowerModel
{
	public StunnedPower()
		: base((PowerType)2, (PowerStackType)2)
	{
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return count;
		}
		return 0m;
	}

	protected override async Task AfterEnergyReset(PlayerChoiceContext ctx, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).Flash();
			await PlayerCmd.SetEnergy(0m, player);
		}
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if ((int)autoPlayType == 0)
		{
			return card.Owner.Creature != ((PowerModel)this).Owner;
		}
		return true;
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
