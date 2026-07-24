using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class SpireGrowth : CustomMonsterModel
{
	private const string QUICK_TACKLE = "QUICK_TACKLE";

	private const string CONSTRICT = "CONSTRICT";

	private const string SMASH = "SMASH";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 190, 170);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 190, 170);

	private int TackleDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 18, 16);

	private int SmashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 25, 22);

	private int ConstrictAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/spire_growth/spire_growth.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("QUICK_TACKLE", (Func<IReadOnlyList<Creature>, Task>)QuickTackle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(TackleDamage) });
		MoveState val2 = new MoveState("CONSTRICT", (Func<IReadOnlyList<Creature>, Task>)Constrict, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val3 = new MoveState("SMASH", (Func<IReadOnlyList<Creature>, Task>)Smash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SmashDamage) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		ICombatState combatState = ((MonsterModel)this).Creature.CombatState;
		Player val = ((combatState != null) ? combatState.Players.FirstOrDefault() : null);
		bool? obj;
		if (val == null)
		{
			obj = null;
		}
		else
		{
			Creature creature = val.Creature;
			obj = ((creature != null) ? new bool?(creature.Powers.Any((PowerModel p) => p is ConstrictedPower)) : ((bool?)null));
		}
		bool? flag = obj;
		bool valueOrDefault = flag == true;
		if (!valueOrDefault && !LastMove(stateMachine, "CONSTRICT"))
		{
			return "CONSTRICT";
		}
		int num = rng.NextInt(100);
		if (num < 50 && !LastTwoMoves(stateMachine, "QUICK_TACKLE"))
		{
			return "QUICK_TACKLE";
		}
		if (!valueOrDefault && !LastMove(stateMachine, "CONSTRICT"))
		{
			return "CONSTRICT";
		}
		if (!LastTwoMoves(stateMachine, "SMASH"))
		{
			return "SMASH";
		}
		return "QUICK_TACKLE";
	}

	private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count == 0)
		{
			return false;
		}
		return stateLog[stateLog.Count - 1].Id == moveId;
	}

	private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 1].Id == moveId && stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task QuickTackle(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)TackleDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Constrict(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<ConstrictedPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)ConstrictAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Smash(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Smash", 0f);
		await Cmd.Wait(0.4f, false);
		await DamageCmd.Attack((decimal)SmashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Attack", false);
		AnimState val3 = new AnimState("Hurt", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("Smash", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(1.3f);
		return val4;
	}
}
