using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class JawWorm : CustomMonsterModel
{
	private bool _hardModeBlockApplied;

	private const string CHOMP = "CHOMP";

	private const string BELLOW = "BELLOW";

	private const string THRASH = "THRASH";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 42, 40);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 46, 44);

	private int ChompDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 11);

	private int ThrashDamage => 7;

	private int ThrashBlock => 5;

	private int BellowStrength => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	private int BellowBlock => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 6);

	public bool HardMode { get; set; } = false;

	protected override string VisualsPath => "res://ActsFromThePast/monsters/jaw_worm/jaw_worm.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
		if (HardMode)
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)BellowStrength, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		await _003C_003En__1(choiceContext, side, participants, combatState);
		if (HardMode && !_hardModeBlockApplied && (int)side == 1)
		{
			_hardModeBlockApplied = true;
			await GainBlockSilent(((MonsterModel)this).Creature, BellowBlock, (ValueProp)8, null);
		}
	}

	private static async Task<decimal> GainBlockSilent(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return default(decimal);
		}
		ICombatState combatState = creature.CombatState;
		await Hook.BeforeBlockGained(combatState, creature, amount, props, (cardPlay != null) ? cardPlay.Card : null);
		IEnumerable<AbstractModel> modifiers = default(IEnumerable<AbstractModel>);
		decimal modifiedAmount = Hook.ModifyBlock(combatState, creature, amount, props, (cardPlay != null) ? cardPlay.Card : null, cardPlay, ref modifiers);
		modifiedAmount = Math.Max(modifiedAmount, 0m);
		await Hook.AfterModifyingBlockAmount(combatState, modifiedAmount, (cardPlay != null) ? cardPlay.Card : null, cardPlay, modifiers);
		if (modifiedAmount > 0m)
		{
			creature.GainBlockInternal(modifiedAmount);
			CombatManager.Instance.History.BlockGained(combatState, creature, (int)modifiedAmount, props, cardPlay);
		}
		await Hook.AfterBlockGained(combatState, creature, modifiedAmount, props, (cardPlay != null) ? cardPlay.Card : null);
		return modifiedAmount;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		AFTPModAudio.Play("jaw_worm", "jaw_worm_death");
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("CHOMP", (Func<IReadOnlyList<Creature>, Task>)Chomp, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(ChompDamage) });
		MoveState val2 = new MoveState("BELLOW", (Func<IReadOnlyList<Creature>, Task>)Bellow, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val3 = new MoveState("THRASH", (Func<IReadOnlyList<Creature>, Task>)Thrash, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(ThrashDamage),
			(AbstractIntent)new DefendIntent()
		});
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		MonsterState val7 = (MonsterState)(object)(HardMode ? conditionalBranchState : ((ConditionalBranchState)(object)val));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, val7);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 25)
		{
			if (LastMove(stateMachine, "CHOMP"))
			{
				return (rng.NextFloat(1f) < 0.5625f) ? "BELLOW" : "THRASH";
			}
			return "CHOMP";
		}
		if (num < 55)
		{
			if (LastTwoMoves(stateMachine, "THRASH"))
			{
				return (rng.NextFloat(1f) < 0.357f) ? "CHOMP" : "BELLOW";
			}
			return "THRASH";
		}
		if (LastMove(stateMachine, "BELLOW"))
		{
			return (rng.NextFloat(1f) < 0.416f) ? "CHOMP" : "THRASH";
		}
		return "BELLOW";
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
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "chomp", 0f);
		await Cmd.Wait(0.6f, false);
		await DamageCmd.Attack((decimal)ChompDamage).FromMonster((MonsterModel)(object)this).WithHitVfxNode((Func<Creature, Node2D>)delegate(Creature target)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (val == null)
			{
				return (Node2D?)null;
			}
			Node2D val2 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_bite")).Instantiate<Node2D>((GenEditState)0);
			val2.GlobalPosition = val.VfxSpawnPosition;
			((CanvasItem)val2).Modulate = new Color(0.3f, 0.5f, 0.7f, 1f);
			return val2;
		})
			.BeforeDamage((Func<Task>)async delegate
			{
				AFTPModAudio.Play("general", "bite", 0f, 0.05f);
			})
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Bellow(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "tailslam", 0f);
		AFTPModAudio.Play(((MonsterModel)this).Creature, "jaw_worm", "jaw_worm_bellow");
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
		}
		await Cmd.Wait(0.5f, false);
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)BellowStrength, ((MonsterModel)this).Creature, (CardModel)null, false);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)BellowBlock, (ValueProp)8, (CardPlay)null, false);
	}

	private async Task Thrash(IReadOnlyList<Creature> targets)
	{
		await HopAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)ThrashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)ThrashBlock, (ValueProp)8, (CardPlay)null, false);
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
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("chomp", false);
		AnimState val3 = new AnimState("tailslam", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("chomp", val2, (Func<bool>)null);
		val4.AddAnyState("tailslam", val3, (Func<bool>)null);
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
	private Task _003C_003En__1(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return ((AbstractModel)this).BeforeSideTurnStart(choiceContext, side, participants, combatState);
	}
}
