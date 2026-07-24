using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class WhisperFromBeyondPower : HexaghostPowerModel
{
	public WhisperFromBeyondPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip((CardKeyword)1);
		((ConstructedPowerModel)this).WithTip<SoulBurnPower>();
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
	{
		if (card.Owner.Creature == ((PowerModel)this).Applier)
		{
			await PowerCmd.Apply<SoulBurnPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, card.Owner.Creature, (CardModel)null, false);
			((PowerModel)this).Flash();
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
