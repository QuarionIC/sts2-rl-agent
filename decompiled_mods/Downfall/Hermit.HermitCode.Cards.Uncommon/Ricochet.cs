using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.History;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Ricochet : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Ricochet()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(8, 2);
		((ConstructedCardModel)this).WithCalculatedVar("CalculatedHits", 0, 1, (Func<CardModel, Creature, decimal>)CountDeadOnEffects, 0, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HermitKeywords.DeadOn));
	}

	private static decimal CountDeadOnEffects(CardModel card, Creature? _)
	{
		return CombatManager.Instance.History.Entries.OfType<DeadOnEntry>().Count((DeadOnEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(card.CombatState) && ((CombatHistoryEntry)e).Actor == card.Owner.Creature);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		int extraHitCount = (int)((CalculatedVar)((CardModel)this).DynamicVars["CalculatedHits"]).Calculate(play.Target);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun2();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		await BetaMainCompatibility.FromCardCompatibility(DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue), (CardModel)(object)this, play).WithHitCount(extraHitCount).TargetingRandomOpponents(((CardModel)this).CombatState, true)
			.WithHermitGunHitFx()
			.BeforeDamage((Func<Task>)delegate
			{
				HermitSfx.PlayGun3();
				return Task.CompletedTask;
			})
			.Execute(ctx);
	}
}
