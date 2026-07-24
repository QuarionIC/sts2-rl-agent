using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Ancient;

[Pool(typeof(SneckoCardPool))]
public class Whiplash : SneckoCardModel, IHasOverflowEffect
{
	public Whiplash()
		: base(2, (CardType)1, (CardRarity)5, (TargetType)3)
	{
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithDamage(12, 4);
		((ConstructedCardModel)(object)this).WithTip<WeakPower>();
		((ConstructedCardModel)(object)this).WithTip<VulnerablePower>();
		((ConstructedCardModel)this).WithCalculatedVar("PowerVar", 0, (Func<CardModel, Creature, decimal>)Calc, 1, 0);
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return card.EnergyCost.GetResolved();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			decimal x = ((CalculatedVar)((CardModel)this).DynamicVars["PowerVar"]).Calculate((Creature)null);
			await PowerCmd.Apply<WeakPower>(ctx, (IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies, x, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			await PowerCmd.Apply<VulnerablePower>(ctx, (IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies, x, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}
	}
}
