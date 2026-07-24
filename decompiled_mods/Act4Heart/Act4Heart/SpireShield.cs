using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Act4Heart;

internal class SpireShield : MonsterModel
{
	private byte state;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 125, 110);

	public override int MaxInitialHp => ((MonsterModel)this).MinInitialHp;

	private static int initial_artifacts => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	private static int bash_damage => 14;

	private static int fortify_block => 30;

	private static int smash_damage => 38;

	public override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		MoveState val = new MoveState("BASH_MOVE", (Func<IReadOnlyList<Creature>, Task>)bash_move, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(bash_damage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val2 = new MoveState("FORTIFY_MOVE", (Func<IReadOnlyList<Creature>, Task>)forify_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val3 = new MoveState("SMASH_MOVE", (Func<IReadOnlyList<Creature>, Task>)smash_move, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(smash_damage),
			(AbstractIntent)new DefendIntent()
		});
		RandomBranchState val4 = new RandomBranchState("RANDOM_BRANCH");
		ConditionalBranchState val5 = new ConditionalBranchState("POST_ATTACK_BRANCH");
		val4.AddBranch((MonsterState)(object)val, (MoveRepeatType)0);
		val4.AddBranch((MonsterState)(object)val2, (MoveRepeatType)0);
		state = 0;
		val.FollowUpState = (MonsterState)(object)val5;
		val2.FollowUpState = (MonsterState)(object)val5;
		val5.AddState((MonsterState)(object)val, (Func<bool>)(() => (state & 1) == 0));
		val5.AddState((MonsterState)(object)val2, (Func<bool>)(() => (state & 2) == 0));
		val5.AddState((MonsterState)(object)val3, (Func<bool>)(() => (state & 3) == 3));
		val3.FollowUpState = (MonsterState)(object)val4;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new _003C_003Ez__ReadOnlyArray<MonsterState>((MonsterState[])(object)new MonsterState[5]
		{
			(MonsterState)val,
			(MonsterState)val2,
			(MonsterState)val3,
			(MonsterState)val4,
			(MonsterState)val5
		}), (MonsterState)(object)val4);
	}

	private async Task bash_move(IReadOnlyList<Creature> targets)
	{
		state |= 1;
		await DamageCmd.Attack((decimal)bash_damage).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3").FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets)
		{
			if (!target.IsPlayer || target.Player.PlayerCombatState.OrbQueue.Capacity <= 0 || !(((MonsterModel)this).Rng.NextFloat(1f) < ModMain.current_config.spire_shield_orbs_focus_down_odds))
			{
				await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, -1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
			else
			{
				await PowerCmd.Apply<FocusPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, -1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
		}
	}

	private async Task forify_move(IReadOnlyList<Creature> targets)
	{
		state |= 2;
		await Task.WhenAll(from a in ((MonsterModel)this).Creature.CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			select CreatureCmd.GainBlock(a, (decimal)fortify_block, (ValueProp)8, (CardPlay)null, false));
	}

	private async Task smash_move(IReadOnlyList<Creature> targets)
	{
		state = 0;
		AttackCommand val = await DamageCmd.Attack((decimal)smash_damage).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3").FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
		if (!AscensionHelper.HasAscension((AscensionLevel)9))
		{
			int num = 0;
			foreach (List<DamageResult> result in val.Results)
			{
				foreach (DamageResult item in result)
				{
					num += item.TotalDamage;
				}
			}
			await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)num, (ValueProp)4, (CardPlay)null, false);
		}
		else
		{
			await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 99m, (ValueProp)8, (CardPlay)null, false);
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await PowerCmd.Apply<BackAttackLeftPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)initial_artifacts, ((MonsterModel)this).Creature, (CardModel)null, false);
	}
}
