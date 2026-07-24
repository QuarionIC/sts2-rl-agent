using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class GiantHead : CustomMonsterModel
{
	private const int CountDamage = 13;

	private const int GlareDuration = 1;

	private const int IncrementDmg = 5;

	private const string GLARE = "GLARE";

	private const string IT_IS_TIME = "IT_IS_TIME";

	private const string COUNT = "COUNT";

	private static readonly LocString[] _timeDialogs = (LocString[])(object)new LocString[4]
	{
		MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog1"),
		MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog2"),
		MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog3"),
		MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog4")
	};

	private int _count;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 520, 500);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 520, 500);

	private int StartingDeathDmg => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 40, 30);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/giant_head/giant_head.tscn";

	private int Count
	{
		get
		{
			return _count;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_count = value;
		}
	}

	private int ItIsTimeDamage => StartingDeathDmg - Count * 5;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_count = AscensionHelper.GetValueIfAscension((AscensionLevel)8, 4, 5);
		await PowerCmd.Apply<SlowPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		MoveState val = new MoveState("GLARE", (Func<IReadOnlyList<Creature>, Task>)Glare, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val2 = new MoveState("IT_IS_TIME", (Func<IReadOnlyList<Creature>, Task>)ItIsTime, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicSingleAttackIntent(() => ItIsTimeDamage) });
		MoveState val3 = new MoveState("COUNT", (Func<IReadOnlyList<Creature>, Task>)CountMove, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(13),
			(AbstractIntent)new DebuffIntent(false)
		});
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2,
			(MonsterState)(object)val3,
			(MonsterState)(object)conditionalBranchState
		}, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (Count <= 1)
		{
			if (Count > -6)
			{
				Count--;
			}
			return "IT_IS_TIME";
		}
		Count--;
		int num = rng.NextInt(100);
		if (num < 50)
		{
			if (!LastTwoMoves(stateMachine, "GLARE"))
			{
				return "GLARE";
			}
			return "COUNT";
		}
		if (!LastTwoMoves(stateMachine, "COUNT"))
		{
			return "COUNT";
		}
		return "GLARE";
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

	private async Task Glare(IReadOnlyList<Creature> targets)
	{
		PlaySfx();
		TalkCmd.Play(GetCountDialog(), ((MonsterModel)this).Creature, (VfxColor)10, (VfxDuration)6);
		await Cmd.Wait(0.5f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task ItIsTime(IReadOnlyList<Creature> targets)
	{
		PlaySfx();
		LocString dialog = _timeDialogs[Rng.Chaotic.NextInt(_timeDialogs.Length)];
		TalkCmd.Play(dialog, ((MonsterModel)this).Creature, (VfxColor)10, (VfxDuration)6);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack((decimal)ItIsTimeDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task CountMove(IReadOnlyList<Creature> targets)
	{
		PlaySfx();
		TalkCmd.Play(GetCountDialog(), ((MonsterModel)this).Creature, (VfxColor)10, (VfxDuration)6);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack(13m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private LocString GetCountDialog()
	{
		LocString val = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.COUNT.dialog");
		val.Add("count", Count.ToString());
		return val;
	}

	private void PlaySfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "giant_head_talk_1", 
			1 => "giant_head_talk_2", 
			_ => "giant_head_talk_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("giant_head", soundName);
	}

	private void PlayDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "giant_head_death_1", 
			1 => "giant_head_death_2", 
			_ => "giant_head_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("giant_head", soundName);
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			PlayDeathSfx();
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		AnimState val = new AnimState("idle_open", true);
		controller.GetAnimationState().SetTimeScale(0.5f);
		return new CreatureAnimator(val, controller);
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
