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
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Centurion : CustomMonsterModel
{
	private const int FuryHits = 3;

	private const string SLASH = "SLASH";

	private const string PROTECT = "PROTECT";

	private const string FURY = "FURY";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 78, 76);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 83, 80);

	private int SlashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 14, 12);

	private int FuryDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 7, 6);

	private int ProtectBlock => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 20, 15);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/centurion/centurion.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
	}

	private void PlayAttackSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "centurion_talk_2" : "centurion_talk_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("centurion", soundName);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SLASH", (Func<IReadOnlyList<Creature>, Task>)Slash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SlashDamage) });
		MoveState val2 = new MoveState("PROTECT", (Func<IReadOnlyList<Creature>, Task>)Protect, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val3 = new MoveState("FURY", (Func<IReadOnlyList<Creature>, Task>)Fury, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(FuryDamage, 3) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		int count = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Count;
		bool flag = count > 1;
		if (num >= 65 && !LastTwoMoves(stateMachine, "PROTECT") && !LastTwoMoves(stateMachine, "FURY"))
		{
			return flag ? "PROTECT" : "FURY";
		}
		if (!LastTwoMoves(stateMachine, "SLASH"))
		{
			return "SLASH";
		}
		return flag ? "PROTECT" : "FURY";
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

	private async Task Slash(IReadOnlyList<Creature> targets)
	{
		PlayAttackSfx();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "MaceSlam", 0f);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack((decimal)SlashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Protect(IReadOnlyList<Creature> targets)
	{
		await Cmd.Wait(0.25f, false);
		IEnumerable<Creature> teammates = from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t;
		Creature target = (teammates.Any() ? ((MonsterModel)this).Rng.NextItem<Creature>(teammates) : ((MonsterModel)this).Creature);
		await CreatureCmd.GainBlock(target, (decimal)ProtectBlock, (ValueProp)8, (CardPlay)null, false);
	}

	private async Task Fury(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 3; i++)
		{
			PlayAttackSfx();
			await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "MaceSlam", 0f);
			await Cmd.Wait(0.3f, false);
			await DamageCmd.Attack((decimal)FuryDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
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
		val4.AddAnyState("MaceSlam", val2, (Func<bool>)null);
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
