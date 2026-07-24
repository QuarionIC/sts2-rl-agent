using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

[Pool(typeof(EventCardPool))]
public sealed class Bite : CustomCardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new DamageVar(7m, (ValueProp)8),
		(DynamicVar)new HealVar(2m)
	};

	public override bool CanBeGeneratedInCombat => false;

	public Bite()
		: base(1, (CardType)1, (CardRarity)6, (TargetType)2, true, true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "Target");
		await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_bite", (string)null, "blunt_attack.mp3")
			.Execute(choiceContext);
		await CreatureCmd.Heal(((CardModel)this).Owner.Creature, ((DynamicVar)((CardModel)this).DynamicVars.Heal).BaseValue, true);
	}

	protected override void OnUpgrade()
	{
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(1m);
		((DynamicVar)((CardModel)this).DynamicVars.Heal).UpgradeValueBy(1m);
	}
}
