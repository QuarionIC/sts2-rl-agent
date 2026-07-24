using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Powers;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Ancient;

public class Crackshot : HermitCardModel, IHasDeadOnEffect, IModifyDamageMultiplicative
{
	public override bool GainsBlock => true;

	public Crackshot()
		: base(1, (CardType)1, (CardRarity)5, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 3);
	}

	public Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public decimal ModifyDamageMultiplicativeCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (this == null)
		{
			return 1m;
		}
		if ((object)cardSource != this || dealer != ((CardModel)this).Owner.Creature || !ValuePropExtensions.IsPoweredAttack(props) || !((IHasDeadOnEffect)this).IsDeadOn)
		{
			return 1m;
		}
		return ((CardModel)this).Owner.Creature.HasPower<SnipePower>() ? 4 : 2;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		int num = (await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun1();
			return Task.CompletedTask;
		})
			.Execute(ctx)).Results.SelectMany((List<DamageResult> e) => e).Sum((DamageResult e) => e.TotalDamage);
		await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, (decimal)num, (ValueProp)8, cardPlay, false);
	}
}
