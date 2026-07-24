using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class TimeEater : CustomMonsterModel
{
	private const int ReverbHits = 3;

	private const int RippleBlock = 20;

	private const int DebuffTurns = 1;

	private const int SlimedCount = 2;

	private const string REVERBERATE = "REVERBERATE";

	private const string RIPPLE = "RIPPLE";

	private const string HEAD_SLAM = "HEAD_SLAM";

	private const string HASTE = "HASTE";

	private static readonly LocString _hasteDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-TIME_EATER.banter.haste");

	private static readonly LocString _introDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-TIME_EATER.banter.intro");

	private bool _usedHaste;

	private bool _firstTurn = true;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 480, 456);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 480, 456);

	private int ReverbDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 8, 7);

	private int HeadSlamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 32, 26);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/time_eater/time_eater.tscn";

	private bool UsedHaste
	{
		get
		{
			return _usedHaste;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedHaste = value;
		}
	}

	private bool FirstTurn
	{
		get
		{
			return _firstTurn;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_firstTurn = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<TimeWarpPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		MoveState val = new MoveState("REVERBERATE", (Func<IReadOnlyList<Creature>, Task>)Reverberate, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(ReverbDamage, 3) });
		MoveState val2 = new MoveState("RIPPLE", (Func<IReadOnlyList<Creature>, Task>)Ripple, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val3 = new MoveState("HEAD_SLAM", (Func<IReadOnlyList<Creature>, Task>)HeadSlam, (AbstractIntent[])(object)new AbstractIntent[3]
		{
			(AbstractIntent)new SingleAttackIntent(HeadSlamDamage),
			(AbstractIntent)new DebuffIntent(false),
			(AbstractIntent)new StatusIntent(2)
		});
		MoveState val4 = new MoveState("HASTE", (Func<IReadOnlyList<Creature>, Task>)Haste, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)))));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2,
			(MonsterState)(object)val3,
			(MonsterState)(object)val4,
			(MonsterState)(object)conditionalBranchState
		}, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (owner.CurrentHp < owner.MaxHp / 2 && !UsedHaste)
		{
			UsedHaste = true;
			return "HASTE";
		}
		int num = rng.NextInt(100);
		if (num < 45)
		{
			if (!LastTwoMoves(stateMachine, "REVERBERATE"))
			{
				return "REVERBERATE";
			}
			num = 50 + rng.NextInt(50);
		}
		if (num < 80)
		{
			if (!LastMove(stateMachine, "HEAD_SLAM"))
			{
				return "HEAD_SLAM";
			}
			return (rng.NextFloat(1f) < 0.66f) ? "REVERBERATE" : "RIPPLE";
		}
		if (!LastMove(stateMachine, "RIPPLE"))
		{
			return "RIPPLE";
		}
		num = rng.NextInt(75);
		if (num < 45 && !LastTwoMoves(stateMachine, "REVERBERATE"))
		{
			return "REVERBERATE";
		}
		if (!LastMove(stateMachine, "HEAD_SLAM"))
		{
			return "HEAD_SLAM";
		}
		return (rng.NextFloat(1f) < 0.66f) ? "REVERBERATE" : "RIPPLE";
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

	private async Task PlayIntroIfFirstTurn()
	{
		if (FirstTurn)
		{
			FirstTurn = false;
			TalkCmd.Play(_introDialog, ((MonsterModel)this).Creature, (VfxColor)3, (VfxDuration)5);
			await Cmd.Wait(0.5f, false);
		}
	}

	private async Task Reverberate(IReadOnlyList<Creature> targets)
	{
		await PlayIntroIfFirstTurn();
		for (int i = 0; i < 3; i++)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
			if (creatureNode != null)
			{
				ShockWaveEffect.PlayRoyal(creatureNode.VfxSpawnPosition);
			}
			await Cmd.Wait(0.75f, false);
			await DamageCmd.Attack((decimal)ReverbDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task Ripple(IReadOnlyList<Creature> targets)
	{
		await PlayIntroIfFirstTurn();
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 20m, (ValueProp)8, (CardPlay)null, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task HeadSlam(IReadOnlyList<Creature> targets)
	{
		await PlayIntroIfFirstTurn();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Slam", 0.4f);
		await DamageCmd.Attack((decimal)HeadSlamDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack", (string)null)
			.WithHitFx("vfx/vfx_slime_impact", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<DrawReductionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		try
		{
			ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
			await CardPileCmd.AddToCombatAndPreview<Slimed>((IEnumerable<Creature>)targets, (PileType)3, 2, (Player)null, (CardPilePosition)1);
		}
		finally
		{
			ClassicSlimedTracker.CreatingClassicSlimed = false;
		}
	}

	private async Task Haste(IReadOnlyList<Creature> targets)
	{
		await PlayIntroIfFirstTurn();
		TalkCmd.Play(_hasteDialog, ((MonsterModel)this).Creature, (VfxColor)3, (VfxDuration)5);
		List<PowerModel> debuffs = ((MonsterModel)this).Creature.Powers.Where((PowerModel p) => (int)p.Type == 2).ToList();
		foreach (PowerModel debuff in debuffs)
		{
			await PowerCmd.Remove(debuff);
		}
		int healAmount = ((MonsterModel)this).Creature.MaxHp / 2 - ((MonsterModel)this).Creature.CurrentHp;
		if (healAmount > 0)
		{
			await CreatureCmd.Heal(((MonsterModel)this).Creature, (decimal)healAmount, true);
		}
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)HeadSlamDamage, (ValueProp)8, (CardPlay)null, false);
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
		val4.AddAnyState("Slam", val2, (Func<bool>)null);
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
