using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class FungiBeast : CustomMonsterModel
{
	private const int VulnerableAmount = 2;

	private const string BITE = "BITE";

	private const string GROW = "GROW";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 24, 22);

	public override int MaxInitialHp => 28;

	private int BiteDamage => 6;

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/fungi_beast/fungi_beast.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<SporeCloudPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		Vector2 position = creatureNode.GetBottomOfHitbox();
		SceneTree tree = default(SceneTree);
		ref SceneTree reference = ref tree;
		MainLoop mainLoop = Engine.GetMainLoop();
		reference = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		SceneTree obj = tree;
		if (obj == null)
		{
			return;
		}
		((GodotObject)obj.CreateTimer(0.009999999776482582, true, false, false)).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			NSporeImpactVfx val = NSporeImpactVfx.Create(position, new Color("8aad7d"));
			if (val != null)
			{
				SceneTree obj2 = tree;
				if (obj2 != null)
				{
					((Node)obj2.Root).AddChild((Node)(object)val, false, (InternalMode)0);
				}
			}
		}), 0u);
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
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BITE", (Func<IReadOnlyList<Creature>, Task>)Bite, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BiteDamage) });
		MoveState val2 = new MoveState("GROW", (Func<IReadOnlyList<Creature>, Task>)Grow, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 60)
		{
			if (LastTwoMoves(stateMachine, "BITE"))
			{
				return "GROW";
			}
			return "BITE";
		}
		if (LastMove(stateMachine, "GROW"))
		{
			return "BITE";
		}
		return "GROW";
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

	private async Task Bite(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack((decimal)BiteDamage).FromMonster((MonsterModel)(object)this).WithAttackerAnim("Attack", 0.5f, (Creature)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Grow(IReadOnlyList<Creature> targets)
	{
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		val4.AddAnyState("Attack", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(Rng.Chaotic.NextFloat(0.7f, 1f));
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
	private Task _003C_003En__1(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
