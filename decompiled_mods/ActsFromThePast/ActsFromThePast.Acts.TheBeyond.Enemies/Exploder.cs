using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Exploder : CustomMonsterModel
{
	private const int ExplosiveCountdown = 3;

	private const int ExplodeDamage = 30;

	private const string ATTACK = "ATTACK";

	private const string EXPLODE = "EXPLODE";

	private bool _hasExploded;

	private int _turnCount;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 30, 30);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 35, 30);

	private int AttackDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 11, 9);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/exploder/exploder.tscn";

	private int TurnCount
	{
		get
		{
			return _turnCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_turnCount = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_turnCount = 0;
		_hasExploded = false;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("ATTACK", (Func<IReadOnlyList<Creature>, Task>)Attack, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(AttackDamage) });
		MoveState val2 = new MoveState("EXPLODE", (Func<IReadOnlyList<Creature>, Task>)Explode, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DeathBlowIntent((Func<decimal>)(() => 30m)) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		TurnCount++;
		return (TurnCount <= 2) ? "ATTACK" : "EXPLODE";
	}

	private async Task Attack(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)AttackDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Explode(IReadOnlyList<Creature> targets)
	{
		if (((MonsterModel)this).Creature.IsDead)
		{
			return;
		}
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "ExplodeTrigger", 0.3f);
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)NFireSmokePuffVfx.Create(((MonsterModel)this).Creature));
		}
		await Cmd.Wait(0.1f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 30m, (ValueProp)8, (CardModel)null, (CardPlay)null);
		}
		await CreatureCmd.Kill(((MonsterModel)this).Creature, false);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("explode", false);
		CreatureAnimator val3 = new CreatureAnimator(val, controller);
		val3.AddAnyState("ExplodeTrigger", val2, (Func<bool>)null);
		return val3;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
