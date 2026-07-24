using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class WrithingMass : CustomMonsterModel
{
	private const int MultiHitCount = 3;

	private const int NormalDebuffAmount = 2;

	private const string BIG_HIT = "BIG_HIT";

	private const string MULTI_HIT = "MULTI_HIT";

	private const string ATTACK_BLOCK = "ATTACK_BLOCK";

	private const string ATTACK_DEBUFF = "ATTACK_DEBUFF";

	private const string MEGA_DEBUFF = "MEGA_DEBUFF";

	private bool _firstMove = true;

	private bool _usedMegaDebuff = false;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 175, 160);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 175, 160);

	private int BigHitDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 38, 32);

	private int MultiHitDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 7);

	private int AttackBlockDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 16, 15);

	private int AttackBlockBlock => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 16, 15);

	private int AttackDebuffDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/writhing_mass/writhing_mass.tscn";

	private bool FirstMove
	{
		get
		{
			return _firstMove;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_firstMove = value;
		}
	}

	public bool UsedMegaDebuff
	{
		get
		{
			return _usedMegaDebuff;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedMegaDebuff = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<ReactivePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<MalleablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Expected O, but got Unknown
		MoveState val = new MoveState("BIG_HIT", (Func<IReadOnlyList<Creature>, Task>)BigHit, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BigHitDamage) });
		MoveState val2 = new MoveState("MULTI_HIT", (Func<IReadOnlyList<Creature>, Task>)MultiHit, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(MultiHitDamage, 3) });
		MoveState val3 = new MoveState("ATTACK_BLOCK", (Func<IReadOnlyList<Creature>, Task>)AttackBlock, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(AttackBlockDamage),
			(AbstractIntent)new DefendIntent()
		});
		MoveState val4 = new MoveState("ATTACK_DEBUFF", (Func<IReadOnlyList<Creature>, Task>)AttackDebuff, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(AttackDebuffDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val5 = new MoveState("MEGA_DEBUFF", (Func<IReadOnlyList<Creature>, Task>)MegaDebuff, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val5.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))))));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2,
			(MonsterState)(object)val3,
			(MonsterState)(object)val4,
			(MonsterState)(object)val5,
			(MonsterState)(object)conditionalBranchState
		}, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (FirstMove)
		{
			FirstMove = false;
			int num = rng.NextInt(100);
			if (num < 33)
			{
				return "MULTI_HIT";
			}
			if (num < 66)
			{
				return "ATTACK_BLOCK";
			}
			return "ATTACK_DEBUFF";
		}
		int num2 = rng.NextInt(100);
		if (num2 < 10)
		{
			if (!LastMove(stateMachine, "BIG_HIT"))
			{
				return "BIG_HIT";
			}
			num2 = 10 + rng.NextInt(90);
		}
		if (num2 < 20)
		{
			if (!UsedMegaDebuff && !LastMove(stateMachine, "MEGA_DEBUFF"))
			{
				UsedMegaDebuff = true;
				return "MEGA_DEBUFF";
			}
			if (rng.NextFloat(1f) < 0.1f && !LastMove(stateMachine, "BIG_HIT"))
			{
				return "BIG_HIT";
			}
			num2 = 20 + rng.NextInt(80);
		}
		if (num2 < 40)
		{
			if (!LastMove(stateMachine, "ATTACK_DEBUFF"))
			{
				return "ATTACK_DEBUFF";
			}
			if (rng.NextFloat(1f) < 0.4f && !LastMove(stateMachine, "BIG_HIT"))
			{
				return "BIG_HIT";
			}
			num2 = 40 + rng.NextInt(60);
		}
		if (num2 < 70)
		{
			if (!LastMove(stateMachine, "MULTI_HIT"))
			{
				return "MULTI_HIT";
			}
			if (rng.NextFloat(1f) < 0.3f)
			{
				return "ATTACK_BLOCK";
			}
			if (!LastMove(stateMachine, "ATTACK_DEBUFF"))
			{
				return "ATTACK_DEBUFF";
			}
			return "BIG_HIT";
		}
		if (!LastMove(stateMachine, "ATTACK_BLOCK"))
		{
			return "ATTACK_BLOCK";
		}
		num2 = rng.NextInt(70);
		if (num2 < 10 && !LastMove(stateMachine, "BIG_HIT"))
		{
			return "BIG_HIT";
		}
		if (num2 < 40 && !LastMove(stateMachine, "ATTACK_DEBUFF"))
		{
			return "ATTACK_DEBUFF";
		}
		return "MULTI_HIT";
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

	private async Task BigHit(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "BigSwing", 0.4f);
		await DamageCmd.Attack((decimal)BigHitDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task MultiHit(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		for (int i = 0; i < 3; i++)
		{
			await DamageCmd.Attack((decimal)MultiHitDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task AttackBlock(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)AttackBlockDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)AttackBlockBlock, (ValueProp)8, (CardPlay)null, false);
	}

	private async Task AttackDebuff(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)AttackDebuffDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task MegaDebuff(IReadOnlyList<Creature> targets)
	{
		UsedMegaDebuff = true;
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
		}
		await Cmd.Wait(0.2f, false);
		foreach (Creature target in targets)
		{
			await CardPileCmd.AddCurseToDeck<Parasite>(target.Player);
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
		val4.AddAnyState("BigSwing", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
