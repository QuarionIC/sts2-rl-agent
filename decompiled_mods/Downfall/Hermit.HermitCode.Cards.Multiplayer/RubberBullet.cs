using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Hermit.HermitCode.Cards.Multiplayer;

public class RubberBullet : HermitCardModel, IHasDeadOnEffect
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public RubberBullet()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithVar("Increase", 7, 2);
	}

	public async Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
		IRunState runState = ((CardModel)this).RunState;
		Player val = ((runState != null) ? runState.Rng.CombatTargets.NextItem<Player>(((IPlayerCollection)((CardModel)this).RunState).Players.Where((Player e) => e.Creature.IsAlive && e != ((CardModel)this).Owner)) : null);
		if (val != null)
		{
			CardModel clone = ((CardModel)this).CreateClone();
			clone._owner = val;
			clone.EnergyCost.AfterCardPlayedCleanup();
			await CardPileCmd.RemoveFromCombat((CardModel)(object)this, false);
			await CardPileCmd.Add(clone, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			HermitSfx.PlayReload();
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun3();
			return Task.CompletedTask;
		})
			.Execute(ctx);
	}
}
