using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class LastChance : HermitCardModel
{
	public LastChance()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithCards(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun3();
			return Task.CompletedTask;
		}).Execute(ctx);
		if (!((CardModel)this).Owner.Creature.Powers.All((PowerModel e) => (int)e.TypeForCurrentAmount != 2))
		{
			await CommonActions.Draw((CardModel)(object)this, ctx);
		}
	}
}
