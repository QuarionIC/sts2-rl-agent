using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class ShadowCloakPower : HermitPowerModel
{
	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && (int)card.Type == 5)
		{
			await CreatureCmd.TriggerAnim(((PowerModel)this).Owner, "Cast", ((PowerModel)this).Owner.Player.Character.CastAnimDelay);
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
		}
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && (int)card.Type == 5)
		{
			await CreatureCmd.TriggerAnim(((PowerModel)this).Owner, "Cast", ((PowerModel)this).Owner.Player.Character.CastAnimDelay);
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
		}
	}

	public ShadowCloakPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
