using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class SongOfSorrowPower : AwakenedPowerModel
{
	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? player)
	{
		if (!(card is Void) || card.Owner != ((PowerModel)this).Owner.Player || !LocalContext.NetId.HasValue)
		{
			return;
		}
		((PowerModel)this).Flash();
		List<Creature> list = ((PowerModel)this).CombatState.Enemies.ToList();
		foreach (Creature item in list)
		{
			if (item != null && item.IsHittable && item.IsAlive)
			{
				await CreatureCmd.Damage(ctx, item, (decimal)((PowerModel)this).Amount, (ValueProp)6, ((PowerModel)this).Owner);
			}
		}
	}

	public SongOfSorrowPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
