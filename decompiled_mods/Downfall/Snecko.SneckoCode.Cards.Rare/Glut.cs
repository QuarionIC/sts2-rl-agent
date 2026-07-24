using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class Glut : SneckoCardModel, IHasOverflowEffect
{
	public Glut()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithDamage(12, 4);
		((ConstructedCardModel)this).WithCalculatedVar("OverflowRepeat", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
		((ConstructedCardModel)this).WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<DamageVar>(new DamageVar("OverflowDamage", 2m, (ValueProp)8), 1m));
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			DamageVar val = (DamageVar)((CardModel)this).DynamicVars["OverflowDamage"];
			int num = (int)((CalculatedVar)((CardModel)this).DynamicVars["OverflowRepeat"]).Calculate((Creature)null);
			if (num != 0)
			{
				await BetaMainCompatibility.FromCardCompatibility(DamageCmd.Attack(((DynamicVar)val).BaseValue), (CardModel)(object)this, cardPlay).TargetingAllOpponents(((CardModel)this).CombatState).WithHitCount(num)
					.Execute(ctx);
			}
		}
	}

	private static decimal Calc(CardModel card, Creature? _)
	{
		return card.Owner.GetHand().Count((CardModel e) => e != card);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
