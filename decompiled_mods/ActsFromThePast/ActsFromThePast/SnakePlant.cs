using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SnakePlant : CustomMonsterModel
{
	private const int ChompHits = 3;

	private const int DebuffAmount = 2;

	private const string CHOMP = "CHOMP";

	private const string SPORES = "SPORES";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 78, 75);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 82, 79);

	private int ChompDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 8, 7);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/snake_plant/snake_plant.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<MalleablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("CHOMP", (Func<IReadOnlyList<Creature>, Task>)ChompMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(ChompDamage, 3) });
		MoveState val2 = new MoveState("SPORES", (Func<IReadOnlyList<Creature>, Task>)SporesMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 65)
		{
			if (LastTwoMoves(stateMachine, "CHOMP"))
			{
				return "SPORES";
			}
			return "CHOMP";
		}
		if (!LastMove(stateMachine, "SPORES") && !LastMoveBefore(stateMachine, "SPORES"))
		{
			return "SPORES";
		}
		return "CHOMP";
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

	private static bool LastMoveBefore(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task ChompMove(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Chomp", 0f);
		await Cmd.Wait(0.5f, false);
		for (int i = 0; i < 3; i++)
		{
			foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
				if (targetNode != null)
				{
					Vector2 position = targetNode.VfxSpawnPosition;
					float offsetX = Rng.Chaotic.NextFloat(1f) * 100f - 50f;
					float offsetY = Rng.Chaotic.NextFloat(1f) * 100f - 50f;
					BiteEffect effect = BiteEffect.Create(position + new Vector2(offsetX, offsetY), (Color?)new Color("7fff00"));
					((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
					((Node2D)effect).GlobalPosition = position + new Vector2(offsetX, offsetY);
				}
			}
			await Cmd.Wait(0.2f, false);
			await DamageCmd.Attack((decimal)ChompDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
		}
	}

	private async Task SporesMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		val4.AddAnyState("Chomp", val2, (Func<bool>)null);
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
