using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SpikeSlimeMedium : CustomMonsterModel
{
	private int? _overrideHp;

	private const int FrailTurns = 1;

	private const int SlimedCount = 1;

	private const string FLAME_TACKLE = "FLAME_TACKLE";

	private const string LICK = "LICK";

	public int? OverrideHp
	{
		get
		{
			return _overrideHp;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_overrideHp = value;
		}
	}

	public override int MinInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension((AscensionLevel)8, 29, 28);

	public override int MaxInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension((AscensionLevel)8, 34, 32);

	private int FlameTackleDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 8);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/spike_slime_medium/spike_slime_medium.tscn";

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)7;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/leaf_slime_m/leaf_slime_m_die", 1f);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("FLAME_TACKLE", (Func<IReadOnlyList<Creature>, Task>)FlameTackle, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(FlameTackleDamage),
			(AbstractIntent)new StatusIntent(1)
		});
		MoveState val2 = new MoveState("LICK", (Func<IReadOnlyList<Creature>, Task>)Lick, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 30)
		{
			if (LastTwoMoves(stateMachine, "FLAME_TACKLE"))
			{
				return "LICK";
			}
			return "FLAME_TACKLE";
		}
		if (LastMove(stateMachine, "LICK"))
		{
			return "FLAME_TACKLE";
		}
		return "LICK";
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

	private async Task FlameTackle(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)FlameTackleDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/leaf_slime_m/leaf_slime_m_attack", (string)null)
			.WithHitFx("vfx/vfx_slime_impact", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		try
		{
			ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
			await CardPileCmd.AddToCombatAndPreview<Slimed>((IEnumerable<Creature>)targets, (PileType)3, 1, (Player)null, (CardPilePosition)1);
		}
		finally
		{
			ClassicSlimedTracker.CreatingClassicSlimed = false;
		}
	}

	private async Task Lick(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("hit", false);
		val2.NextState = val;
		CreatureAnimator val3 = new CreatureAnimator(val, controller);
		val3.AddAnyState("Hit", val2, (Func<bool>)null);
		return val3;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
