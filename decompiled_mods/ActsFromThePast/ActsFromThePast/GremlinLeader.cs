using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class GremlinLeader : CustomMonsterModel
{
	private const int StabDamage = 6;

	private const int StabHits = 3;

	private static readonly LocString _encourageLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.moves.ENCOURAGE.dialog1");

	private static readonly LocString _encourageLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.moves.ENCOURAGE.dialog2");

	private static readonly LocString _encourageLine3 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.moves.ENCOURAGE.dialog3");

	private static readonly LocString _gremlinFleeeLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee1");

	private static readonly LocString _gremlinFleeLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee2");

	private const string RALLY = "RALLY";

	private const string ENCOURAGE = "ENCOURAGE";

	private const string STAB = "STAB";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 145, 140);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 155, 148);

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 4);

	private int BlockAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 6);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_leader/gremlin_leader.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		foreach (Creature teammate in from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature
			select t)
		{
			await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), teammate, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		foreach (Creature teammate in from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t)
		{
			await PowerCmd.Remove<MinionPower>(teammate);
		}
	}

	private int NumAliveGremlins()
	{
		return ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Count((Creature t) => t != ((MonsterModel)this).Creature && t.IsAlive);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("RALLY", (Func<IReadOnlyList<Creature>, Task>)Rally, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SummonIntent() });
		MoveState val2 = new MoveState("ENCOURAGE", (Func<IReadOnlyList<Creature>, Task>)Encourage, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val3 = new MoveState("STAB", (Func<IReadOnlyList<Creature>, Task>)Stab, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(6, 3) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = NumAliveGremlins();
		int num2 = rng.NextInt(100);
		if (num == 0)
		{
			if (num2 < 75)
			{
				if (!LastMove(stateMachine, "RALLY"))
				{
					return "RALLY";
				}
				return "STAB";
			}
			if (!LastMove(stateMachine, "STAB"))
			{
				return "STAB";
			}
			return "RALLY";
		}
		if (num < 2)
		{
			if (num2 < 50)
			{
				if (!LastMove(stateMachine, "RALLY"))
				{
					return "RALLY";
				}
				return SelectFromUpperRange(rng, stateMachine);
			}
			return SelectFromUpperRange(rng, stateMachine);
		}
		if (num2 < 66)
		{
			if (!LastMove(stateMachine, "ENCOURAGE"))
			{
				return "ENCOURAGE";
			}
			return "STAB";
		}
		if (!LastMove(stateMachine, "STAB"))
		{
			return "STAB";
		}
		return "ENCOURAGE";
	}

	private string SelectFromUpperRange(Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 60)
		{
			if (!LastMove(stateMachine, "ENCOURAGE"))
			{
				return "ENCOURAGE";
			}
			return "STAB";
		}
		if (!LastMove(stateMachine, "STAB"))
		{
			return "STAB";
		}
		int num2 = rng.NextInt(80);
		if (num2 < 50)
		{
			if (!LastMove(stateMachine, "RALLY"))
			{
				return "RALLY";
			}
			return "ENCOURAGE";
		}
		if (!LastMove(stateMachine, "ENCOURAGE"))
		{
			return "ENCOURAGE";
		}
		return "STAB";
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

	private async Task Rally(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Call", 0f);
		HashSet<string> occupiedSlots = (from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t.IsAlive
			select t.SlotName).ToHashSet();
		for (int i = 0; i < 2; i++)
		{
			string emptySlot = ((MonsterModel)this).CombatState.Encounter.Slots.Where((string s) => s != "leader" && !occupiedSlots.Contains(s)).LastOrDefault();
			if (emptySlot == null)
			{
				break;
			}
			Creature summoned = await SummonRandomGremlin(emptySlot);
			if (summoned != null)
			{
				occupiedSlots.Add(emptySlot);
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature node = ((instance != null) ? instance.GetCreatureNode(summoned) : null);
				if (node != null)
				{
					((CanvasItem)node).Visible = false;
				}
				await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), summoned, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
				await SummonSlideInAnimation.Play(summoned);
			}
		}
	}

	private async Task<Creature?> SummonRandomGremlin(string slotName)
	{
		int roll = ((MonsterModel)this).Rng.NextInt(8);
		if (1 == 0)
		{
		}
		Creature result;
		switch (roll)
		{
		case 0:
		case 1:
			result = await CreatureCmd.Add<GremlinMad>(((MonsterModel)this).CombatState, slotName);
			break;
		case 2:
		case 3:
			result = await CreatureCmd.Add<GremlinSneaky>(((MonsterModel)this).CombatState, slotName);
			break;
		case 4:
		case 5:
			result = await CreatureCmd.Add<GremlinFat>(((MonsterModel)this).CombatState, slotName);
			break;
		case 6:
			result = await CreatureCmd.Add<GremlinShield>(((MonsterModel)this).CombatState, slotName);
			break;
		default:
			result = await CreatureCmd.Add<GremlinWizard>(((MonsterModel)this).CombatState, slotName);
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task Encourage(IReadOnlyList<Creature> targets)
	{
		LocString[] encourageLines = (LocString[])(object)new LocString[3] { _encourageLine1, _encourageLine2, _encourageLine3 };
		LocString line = encourageLines[Rng.Chaotic.NextInt(encourageLines.Length)];
		TalkCmd.Play(line, ((MonsterModel)this).Creature, (VfxColor)8, (VfxDuration)4);
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		foreach (Creature teammate in from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t)
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), teammate, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
			await CreatureCmd.GainBlock(teammate, (decimal)BlockAmount, (ValueProp)8, (CardPlay)null, false);
		}
	}

	private async Task Stab(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Multislash", 0f);
		await Cmd.Wait(0.5f, false);
		for (int i = 0; i < 3; i++)
		{
			await DamageCmd.Attack(6m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
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
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Attack", false);
		AnimState val3 = new AnimState("Call", false);
		AnimState val4 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = val;
		CreatureAnimator val5 = new CreatureAnimator(val, controller);
		val5.AddAnyState("Multislash", val2, (Func<bool>)null);
		val5.AddAnyState("Call", val3, (Func<bool>)null);
		val5.AddAnyState("Hit", val4, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(0.8f);
		return val5;
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
