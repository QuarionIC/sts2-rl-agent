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

namespace ActsFromThePast;

public sealed class Romeo : CustomMonsterModel
{
	private const int WeakAmount = 3;

	private static readonly LocString _mockBearAlive = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-ROMEO.moves.MOCK.bearAlive");

	private static readonly LocString _mockBearDead = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-ROMEO.moves.MOCK.bearDead");

	private static readonly LocString _deathReactLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-ROMEO.deathReactLine");

	private const string CROSS_SLASH = "CROSS_SLASH";

	private const string MOCK = "MOCK";

	private const string AGONIZING_SLASH = "AGONIZING_SLASH";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 37, 35);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 41, 39);

	private int CrossSlashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 17, 15);

	private int AgonizeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/romeo/romeo.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		Creature bear = ((IEnumerable<Creature>)((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.Monster is Bear));
		if (bear != null)
		{
			bear.Died += BearDeathResponse;
		}
	}

	private void BearDeathResponse(Creature _)
	{
		_.Died -= BearDeathResponse;
		if (!((MonsterModel)this).Creature.IsDead)
		{
			TalkCmd.Play(_deathReactLine, ((MonsterModel)this).Creature, (VfxColor)0, (VfxDuration)4);
		}
	}

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
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("MOCK", (Func<IReadOnlyList<Creature>, Task>)Mock, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		MoveState val2 = new MoveState("CROSS_SLASH", (Func<IReadOnlyList<Creature>, Task>)CrossSlash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(CrossSlashDamage) });
		MoveState val3 = new MoveState("AGONIZING_SLASH", (Func<IReadOnlyList<Creature>, Task>)AgonizingSlash, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(AgonizeDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
		val.FollowUpState = (MonsterState)(object)val3;
		val3.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val2.FollowUpState = (MonsterState)(object)conditionalBranchState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (!LastTwoMoves(stateMachine, "CROSS_SLASH"))
		{
			return "CROSS_SLASH";
		}
		return "AGONIZING_SLASH";
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

	private async Task Mock(IReadOnlyList<Creature> targets)
	{
		LocString line = (((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Any((Creature t) => t != ((MonsterModel)this).Creature && t.IsAlive && t.Monster is Bear) ? _mockBearAlive : _mockBearDead);
		TalkCmd.Play(line, ((MonsterModel)this).Creature, (VfxColor)0, (VfxDuration)4);
	}

	private async Task CrossSlash(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Stab", 0f);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack((decimal)CrossSlashDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task AgonizingSlash(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Stab", 0f);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack((decimal)AgonizeDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		val4.AddAnyState("Stab", val2, (Func<bool>)null);
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
