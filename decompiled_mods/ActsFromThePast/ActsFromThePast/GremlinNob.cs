using System;
using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinNob : CustomMonsterModel
{
	private const int VulnerableAmount = 2;

	private const string RUSH = "RUSH";

	private const string SKULL_BASH = "SKULL_BASH";

	private const string BELLOW = "BELLOW";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 85, 82);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 90, 86);

	private int RushDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 16, 14);

	private int BashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 8, 6);

	private int EnrageAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 3, 2);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_nob/gremlin_nob.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BELLOW", (Func<IReadOnlyList<Creature>, Task>)Bellow, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val2 = new MoveState("RUSH", (Func<IReadOnlyList<Creature>, Task>)Rush, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(RushDamage) });
		MoveState val3 = new MoveState("SKULL_BASH", (Func<IReadOnlyList<Creature>, Task>)SkullBash, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(BashDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		ConditionalBranchState item = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (!LastMove(stateMachine, "SKULL_BASH") && !LastMoveBefore(stateMachine, "SKULL_BASH"))
		{
			return "SKULL_BASH";
		}
		if (LastTwoMoves(stateMachine, "RUSH"))
		{
			return "SKULL_BASH";
		}
		return "RUSH";
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

	private static bool LastMoveBefore(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 2].Id == moveId;
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

	private async Task Bellow(IReadOnlyList<Creature> targets)
	{
		PlayBellowSfx();
		TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_NOB.moves.BELLOW.banter"), ((MonsterModel)this).Creature, (VfxColor)0, (VfxDuration)5);
		VfxCmd.PlayOnCreatureCenter(((MonsterModel)this).Creature, "vfx/vfx_scream");
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)4, (ShakeDuration)3, -1f);
		}
		await Cmd.Wait(0.8f, false);
		int enrage = ((((MonsterModel)this).Creature.CombatState.Players.Count > 2) ? 1 : EnrageAmount);
		await PowerCmd.Apply<EnragePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)enrage, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Rush(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)RushDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
		}
	}

	private async Task SkullBash(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)BashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_gaze", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private void PlayBellowSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "bellow_1", 
			1 => "bellow_2", 
			_ => "bellow_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_nob", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("animation", true);
		CreatureAnimator result = new CreatureAnimator(val, controller);
		MegaAnimationState animationState = controller.GetAnimationState();
		MegaTrackEntry current = animationState.GetCurrent(0);
		current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
		animationState.Update(0f);
		animationState.Apply(controller.GetSkeleton());
		return result;
	}
}
