using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class SharpHidePower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public bool AttackInProgress { get; private set; }

	public Creature AttackSource { get; private set; }

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)cardPlay.Card.Type == 1)
		{
			AttackInProgress = true;
			Player owner = cardPlay.Card.Owner;
			AttackSource = ((owner != null) ? owner.Creature : null);
		}
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		AttackInProgress = false;
		AttackSource = null;
		if ((int)cardPlay.Card.Type == 1)
		{
			((PowerModel)this).Flash();
			Player owner = cardPlay.Card.Owner;
			Creature player = ((owner != null) ? owner.Creature : null);
			if (player != null && player.IsAlive)
			{
				await CreatureCmd.Damage(choiceContext, player, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardModel)null, (CardPlay)null);
			}
		}
	}
}
