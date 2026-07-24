using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Act4Heart;

internal class SpireSpear : MonsterModel
{
	private byte state;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 180, 160);

	public override int MaxInitialHp => ((MonsterModel)this).MinInitialHp;

	private static int initial_artifacts => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	private static int burn_strike_damage => 6;

	private static int burn_strike_count => 2;

	private static PileType burn_strike_pile
	{
		get
		{
			if (AscensionHelper.HasAscension((AscensionLevel)9))
			{
				return (PileType)1;
			}
			return (PileType)3;
		}
	}

	private static int skewer_damage => 10;

	private static int skewer_count => 4;

	private static int piercer_amount => 2;

	public override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		MoveState val = new MoveState("BURN_STRIKE_MOVE", (Func<IReadOnlyList<Creature>, Task>)burn_strike_move, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new MultiAttackIntent(burn_strike_damage, burn_strike_count),
			(AbstractIntent)new StatusIntent(2)
		});
		MoveState val2 = new MoveState("SKEWER_MOVE", (Func<IReadOnlyList<Creature>, Task>)skewer_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(skewer_damage, skewer_count) });
		MoveState val3 = new MoveState("PIERCER_MOVE", (Func<IReadOnlyList<Creature>, Task>)piercer_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		RandomBranchState val4 = new RandomBranchState("RANDOM_BRANCH");
		ConditionalBranchState val5 = new ConditionalBranchState("POST_ATTACK_BRANCH");
		state = 2;
		val.FollowUpState = (MonsterState)(object)val5;
		val5.AddState((MonsterState)(object)val, (Func<bool>)(() => (state & 1) == 0));
		val5.AddState((MonsterState)(object)val3, (Func<bool>)(() => (state & 2) == 0));
		val5.AddState((MonsterState)(object)val2, (Func<bool>)(() => (state & 3) == 3));
		val2.FollowUpState = (MonsterState)(object)val4;
		val4.AddBranch((MonsterState)(object)val, (MoveRepeatType)0);
		val4.AddBranch((MonsterState)(object)val3, (MoveRepeatType)0);
		val3.FollowUpState = (MonsterState)(object)val5;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new _003C_003Ez__ReadOnlyArray<MonsterState>((MonsterState[])(object)new MonsterState[5]
		{
			(MonsterState)val,
			(MonsterState)val2,
			(MonsterState)val3,
			(MonsterState)val4,
			(MonsterState)val5
		}), (MonsterState)(object)val);
	}

	private async Task burn_strike_move(IReadOnlyList<Creature> targets)
	{
		state |= 1;
		await DamageCmd.Attack((decimal)burn_strike_damage).WithHitCount(burn_strike_count).WithHitFx("vfx/vfx_attack_slash", (string)null, "slash_attack.mp3")
			.FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
		await CardPileCmd.AddToCombatAndPreview<Burn>((IEnumerable<Creature>)targets, burn_strike_pile, 2, (Player)null, (CardPilePosition)2);
	}

	private async Task skewer_move(IReadOnlyList<Creature> targets)
	{
		state = 0;
		await DamageCmd.Attack((decimal)skewer_damage).WithHitCount(skewer_count).WithHitFx("vfx/vfx_attack_slash", (string)null, "slash_attack.mp3")
			.FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task piercer_move(IReadOnlyList<Creature> targets)
	{
		state |= 2;
		await Task.WhenAll(from a in ((MonsterModel)this).Creature.CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			select PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), a, (decimal)piercer_amount, ((MonsterModel)this).Creature, (CardModel)null, false));
	}

	public override async Task AfterAddedToRoom()
	{
		await PowerCmd.Apply<SurroundedPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)((MonsterModel)this).CombatState.GetOpponentsOf(((MonsterModel)this).Creature), 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<BackAttackRightPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)initial_artifacts, ((MonsterModel)this).Creature, (CardModel)null, false);
	}
}
