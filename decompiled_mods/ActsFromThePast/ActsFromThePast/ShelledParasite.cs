using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class ShelledParasite : CustomMonsterModel
{
	private const int PlatedArmorAmount = 14;

	private const int DoubleStrikeCount = 2;

	private const int FellFrailAmount = 2;

	private const string FELL = "FELL";

	private const string DOUBLE_STRIKE = "DOUBLE_STRIKE";

	private const string LIFE_SUCK = "LIFE_SUCK";

	private const string STUNNED = "STUNNED";

	private MoveState _stunnedState;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 70, 68);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 75, 72);

	private int FellDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 21, 18);

	private int DoubleStrikeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 7, 6);

	private int SuckDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/shelled_parasite/shelled_parasite.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<PlatedArmorPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 14m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("FELL", (Func<IReadOnlyList<Creature>, Task>)Fell, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(FellDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val2 = new MoveState("DOUBLE_STRIKE", (Func<IReadOnlyList<Creature>, Task>)DoubleStrike, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(DoubleStrikeDamage, 2) });
		MoveState val3 = new MoveState("LIFE_SUCK", (Func<IReadOnlyList<Creature>, Task>)LifeSuck, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(SuckDamage),
			(AbstractIntent)new HealIntent()
		});
		_stunnedState = new MoveState("STUNNED", (Func<IReadOnlyList<Creature>, Task>)Stunned, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StunIntent() });
		ConditionalBranchState item = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		_stunnedState.FollowUpState = (MonsterState)(object)val;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)_stunnedState);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 20)
		{
			if (!LastMove(stateMachine, "FELL"))
			{
				return "FELL";
			}
			return SelectNextMove(owner, rng, stateMachine, 20);
		}
		if (num < 60)
		{
			if (!LastTwoMoves(stateMachine, "DOUBLE_STRIKE"))
			{
				return "DOUBLE_STRIKE";
			}
			return "LIFE_SUCK";
		}
		if (!LastTwoMoves(stateMachine, "LIFE_SUCK"))
		{
			return "LIFE_SUCK";
		}
		return "DOUBLE_STRIKE";
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine, int min)
	{
		int num = rng.NextInt(min, 100);
		if (num < 60)
		{
			if (!LastTwoMoves(stateMachine, "DOUBLE_STRIKE"))
			{
				return "DOUBLE_STRIKE";
			}
			return "LIFE_SUCK";
		}
		if (!LastTwoMoves(stateMachine, "LIFE_SUCK"))
		{
			return "LIFE_SUCK";
		}
		return "DOUBLE_STRIKE";
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

	private async Task Fell(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)FellDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task DoubleStrike(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 2; i++)
		{
			await HopAnimation.Play(((MonsterModel)this).Creature);
			await Cmd.Wait(0.2f, false);
			await DamageCmd.Attack((decimal)DoubleStrikeDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task LifeSuck(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Bite", 0f);
		await Cmd.Wait(0.4f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (targetNode != null)
			{
				Vector2 position = targetNode.VfxSpawnPosition;
				BiteEffect effect = BiteEffect.Create(position);
				((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
				((Node2D)effect).GlobalPosition = position;
			}
		}
		await Cmd.Wait(0.3f, false);
		int totalUnblocked = (from r in (await DamageCmd.Attack((decimal)SuckDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null)).Results.SelectMany((List<DamageResult> r) => r)
			where r != null
			select r).Sum((DamageResult r) => r.UnblockedDamage);
		if (totalUnblocked > 0)
		{
			await CreatureCmd.Heal(((MonsterModel)this).Creature, (decimal)totalUnblocked, true);
		}
	}

	private async Task Stunned(IReadOnlyList<Creature> targets)
	{
		await Cmd.Wait(0.5f, false);
	}

	public async Task OnArmorBreak()
	{
		await HopAnimation.Play(((MonsterModel)this).Creature);
		await Cmd.Wait(0.3f, false);
		await HopAnimation.Play(((MonsterModel)this).Creature);
		await Cmd.Wait(0.3f, false);
		await HopAnimation.Play(((MonsterModel)this).Creature);
		((MonsterModel)this).SetMoveImmediate(_stunnedState, true);
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
		val4.AddAnyState("Bite", val2, (Func<bool>)null);
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

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__1(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
