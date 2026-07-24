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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Darkling : CustomMonsterModel
{
	private const int ChompHits = 2;

	private const int HardenBlock = 12;

	private const string CHOMP = "CHOMP";

	private const string HARDEN = "HARDEN";

	private const string NIP = "NIP";

	private const string DEAD_MOVE = "DEAD_MOVE";

	private const string REATTACH_MOVE = "REATTACH_MOVE";

	private bool _firstMove = true;

	private int _slotIndex;

	private MoveState _deadState;

	private readonly Dictionary<Creature, int> _nipDamageByCreature = new Dictionary<Creature, int>();

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 50, 48);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 59, 56);

	private int ChompDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 8);

	private int HardenStrength => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 0);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/darkling/darkling.tscn";

	public bool FirstMove
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

	public int SlotIndex
	{
		get
		{
			return _slotIndex;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_slotIndex = value;
		}
	}

	public MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		private set
		{
			((AbstractModel)this).AssertMutable();
			_deadState = value;
		}
	}

	public override bool ShouldFadeAfterDeath => false;

	public override bool ShouldDisappearFromDoom => false;

	private int GetNipDamage()
	{
		int value;
		return _nipDamageByCreature.TryGetValue(((MonsterModel)this).Creature, out value) ? value : 0;
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_nipDamageByCreature[((MonsterModel)this).Creature] = (AscensionHelper.HasAscension((AscensionLevel)9) ? ((MonsterModel)this).RunRng.MonsterAi.NextInt(9, 14) : ((MonsterModel)this).RunRng.MonsterAi.NextInt(7, 12));
		int healAmount = ((MonsterModel)this).Creature.MaxHp / 2;
		await PowerCmd.Apply<LifeLinkPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)healAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		AbstractIntent[] array = (AbstractIntent[])(object)((HardenStrength <= 0) ? new AbstractIntent[1] { (AbstractIntent)new DefendIntent() } : new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val = new MoveState("CHOMP", (Func<IReadOnlyList<Creature>, Task>)Chomp, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(ChompDamage, 2) });
		MoveState val2 = new MoveState("HARDEN", (Func<IReadOnlyList<Creature>, Task>)Harden, array);
		MoveState val3 = new MoveState("NIP", (Func<IReadOnlyList<Creature>, Task>)Nip, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicSingleAttackIntent(() => GetNipDamage()) });
		DeadState = new MoveState("DEAD_MOVE", (Func<IReadOnlyList<Creature>, Task>)DeadMove, Array.Empty<AbstractIntent>());
		MoveState val4 = new MoveState("REATTACH_MOVE", (Func<IReadOnlyList<Creature>, Task>)ReattachMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new HealIntent() })
		{
			MustPerformOnceBeforeTransitioning = true
		};
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		DeadState.FollowUpState = (MonsterState)(object)val4;
		val4.FollowUpState = (MonsterState)(object)conditionalBranchState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)DeadState);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (FirstMove)
		{
			FirstMove = false;
			int num = rng.NextInt(100);
			return (num < 50) ? "HARDEN" : "NIP";
		}
		int num2 = rng.NextInt(100);
		if (num2 < 40)
		{
			if (!LastMove(stateMachine, "CHOMP") && SlotIndex % 2 == 0)
			{
				return "CHOMP";
			}
			return SelectNextMove(owner, rng, stateMachine, rng.NextInt(60) + 40);
		}
		if (num2 < 70)
		{
			if (!LastMove(stateMachine, "HARDEN"))
			{
				return "HARDEN";
			}
			return "NIP";
		}
		if (!LastTwoMoves(stateMachine, "NIP"))
		{
			return "NIP";
		}
		return SelectNextMove(owner, rng, stateMachine, rng.NextInt(100));
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine, int forcedRoll)
	{
		if (forcedRoll < 40)
		{
			if (!LastMove(stateMachine, "CHOMP") && SlotIndex % 2 == 0)
			{
				return "CHOMP";
			}
			return (forcedRoll < 20) ? "HARDEN" : "NIP";
		}
		if (forcedRoll < 70)
		{
			if (!LastMove(stateMachine, "HARDEN"))
			{
				return "HARDEN";
			}
			return "NIP";
		}
		if (!LastTwoMoves(stateMachine, "NIP"))
		{
			return "NIP";
		}
		return (!LastMove(stateMachine, "HARDEN")) ? "HARDEN" : "CHOMP";
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

	private async Task Chomp(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Chomp", 0f);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack((decimal)ChompDamage).WithHitCount(2).FromMonster((MonsterModel)(object)this)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Harden(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 12m, (ValueProp)8, (CardPlay)null, false);
		if (HardenStrength > 0)
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)HardenStrength, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Nip(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)GetNipDamage()).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private Task DeadMove(IReadOnlyList<Creature> targets)
	{
		return Task.CompletedTask;
	}

	private async Task ReattachMove(IReadOnlyList<Creature> targets)
	{
		string sfxName = ((Rng.Chaotic.NextInt(2) == 0) ? "darkling_regrow_1" : "darkling_regrow_2");
		AFTPModAudio.Play("darkling", sfxName);
		LifeLinkPower regrowPower = ((MonsterModel)this).Creature.Powers.OfType<LifeLinkPower>().FirstOrDefault();
		if (regrowPower != null)
		{
			await regrowPower.DoReattach();
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
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Attack", false);
		AnimState val3 = new AnimState("Hit", false);
		AnimState nextState = new AnimState("dead_loop", true);
		AnimState val4 = new AnimState("wither", false);
		AnimState val5 = new AnimState("regenerate", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = nextState;
		val5.NextState = val;
		CreatureAnimator val6 = new CreatureAnimator(val, controller);
		val6.AddAnyState("Chomp", val2, (Func<bool>)null);
		val6.AddAnyState("Hit", val3, (Func<bool>)null);
		val6.AddAnyState("Dead", val4, (Func<bool>)null);
		val6.AddAnyState("Revive", val5, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(Rng.Chaotic.NextFloat(0.75f, 1f));
		return val6;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
