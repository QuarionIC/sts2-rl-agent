using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Mystic : CustomMonsterModel
{
	private const int FrailAmount = 2;

	private const string ATTACK = "ATTACK";

	private const string HEAL = "HEAL";

	private const string BUFF = "BUFF";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 50, 48);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 58, 56);

	private int MagicDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 8);

	private int HealAmount => 20 * ((MonsterModel)this).CombatState.Players.Count;

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 4, 3);

	private int HealThreshold => 20 * ((MonsterModel)this).CombatState.Players.Count;

	protected override string VisualsPath => "res://ActsFromThePast/monsters/mystic/mystic.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		PlayDeathSfx();
	}

	private void PlayDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "mystic_death_1", 
			1 => "mystic_death_2", 
			_ => "mystic_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("mystic", soundName);
	}

	private void PlayTurnSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "mystic_talk_2" : "mystic_talk_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("mystic", soundName);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("ATTACK", (Func<IReadOnlyList<Creature>, Task>)Attack, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(MagicDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val2 = new MoveState("HEAL", (Func<IReadOnlyList<Creature>, Task>)Heal, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new HealIntent() });
		MoveState val3 = new MoveState("BUFF", (Func<IReadOnlyList<Creature>, Task>)Buff, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		IReadOnlyList<Creature> teammatesOf = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature);
		int num = 0;
		foreach (Creature item in teammatesOf)
		{
			if (item.IsAlive)
			{
				num += item.MaxHp - item.CurrentHp;
			}
		}
		if (num > HealThreshold && !LastTwoMoves(stateMachine, "HEAL"))
		{
			return "HEAL";
		}
		int num2 = rng.NextInt(100);
		if (num2 >= 40 && !LastMove(stateMachine, "ATTACK"))
		{
			return "ATTACK";
		}
		if (!LastTwoMoves(stateMachine, "BUFF"))
		{
			return "BUFF";
		}
		return "ATTACK";
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

	private async Task Attack(IReadOnlyList<Creature> targets)
	{
		PlayTurnSfx();
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)MagicDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)targets, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Heal(IReadOnlyList<Creature> targets)
	{
		PlayTurnSfx();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Heal", 0f);
		await Cmd.Wait(0.25f, false);
		IReadOnlyList<Creature> teammates = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature);
		foreach (Creature teammate in teammates)
		{
			if (teammate.IsAlive)
			{
				await CreatureCmd.Heal(teammate, (decimal)HealAmount, true);
			}
		}
	}

	private async Task Buff(IReadOnlyList<Creature> targets)
	{
		PlayTurnSfx();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Heal", 0f);
		await Cmd.Wait(0.25f, false);
		IReadOnlyList<Creature> teammates = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature);
		foreach (Creature teammate in teammates)
		{
			if (teammate.IsAlive)
			{
				await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), teammate, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
		}
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
		AnimState val3 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("Heal", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(0.8f);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
