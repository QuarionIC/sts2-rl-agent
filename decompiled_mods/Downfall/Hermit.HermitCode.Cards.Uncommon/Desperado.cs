using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Desperado : HermitCardModel, IModifyDamageMultiplicative
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Desperado()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(10, 2);
		((ConstructedCardModel)this).WithVar("PlayCountMultiplier", 1, 0);
		((ConstructedCardModel)this).WithVar("CostIncrease", 1, 0);
	}

	public decimal ModifyDamageMultiplicativeCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if ((object)cardSource != this || dealer != ((CardModel)this).Owner.Creature || !ValuePropExtensions.IsPoweredAttack(props))
		{
			return 1m;
		}
		return ((CardModel)this).DynamicVars["PlayCountMultiplier"].BaseValue;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun2();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		DynamicVar obj = ((CardModel)this).DynamicVars["PlayCountMultiplier"];
		obj.BaseValue *= 2m;
		((CardModel)this).EnergyCost.AddThisCombat(((CardModel)this).DynamicVars["CostIncrease"].IntValue, false);
	}
}
