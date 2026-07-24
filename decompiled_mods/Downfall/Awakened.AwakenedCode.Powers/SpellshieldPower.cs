using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class SpellshieldPower : AwakenedPowerModel
{
	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Owner.Player != null)
		{
			int a = ((PowerModel)this).Owner.Player.GetHand().Count((CardModel e) => e.ShouldRetainThisTurn);
			for (int i = 0; i < a; i++)
			{
				await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
				((PowerModel)this).Flash();
			}
		}
	}

	public SpellshieldPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
