using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Ghostflames.Intents;
using Hexaghost.HexaghostCode.Powers;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Ghostflames;

public class InfernoGhostflame : GhostflameModel
{
	protected override int IgnitionRequirement => 3;

	public override FireColor FireColor => FireColor.Red;

	public override AbstractIntent Intent => (AbstractIntent)(object)new CustomAttackIntent(() => 4 + base.Intensity, () => HexaghostCmd.GetIgnitedCount(base.Owner) + ((!base.IsIgnited) ? 1 : 0) * (1 + Repeat(GhostflameRepeatType.Damage)));

	public override async Task OnIgnite(PlayerChoiceContext ctx)
	{
		if (base.Owner.Creature.CombatState == null)
		{
			return;
		}
		int ignitedCount = HexaghostCmd.GetIgnitedCount(base.Owner);
		SfxCmd.Play("event:/sfx/characters/attack_fire", 1f);
		int hitCount = ignitedCount + Repeat(GhostflameRepeatType.Damage);
		int damage = 4 + base.Intensity;
		for (int i = 0; i < hitCount; i++)
		{
			Creature val = base.CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)base.CombatState.HittableEnemies);
			if (val != null)
			{
				SpawnVfx(val);
				if (val.IsHittable)
				{
					await CreatureCmd.Damage(ctx, val, (decimal)damage, (ValueProp)12, base.Owner.Creature);
				}
			}
		}
		if (HexaghostCmd.AllIgnited(base.Owner))
		{
			await PowerCmd.Apply<IntensityPower>(ctx, base.Owner.Creature, 2m, base.Owner.Creature, (CardModel)null, false);
		}
		await Cmd.Wait(0.2f, false);
		await HexaghostCmd.ExtinguishAllExceptThis(ctx, base.Owner, this);
	}

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(base.Owner.Creature) || !base.IsIgnited)
		{
			return Task.CompletedTask;
		}
		Extinguish();
		HexaghostVisualsBridge.Refresh(base.Owner);
		return Task.CompletedTask;
	}

	protected override async Task AfterEnergySpent(PlayerChoiceContext ctx, CardModel card, int amount)
	{
		if (base.IsActive && card.Owner == base.Owner && LocalContext.NetId.HasValue && TryProgress(amount))
		{
			await Ignite(ctx);
		}
	}
}
