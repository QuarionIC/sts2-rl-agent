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

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Reptomancer : CustomMonsterModel
{
	private const int SnakeStrikeHits = 2;

	private const int DaggersPerSpawn = 2;

	private const string SNAKE_STRIKE = "SNAKE_STRIKE";

	private const string SPAWN_DAGGER = "SPAWN_DAGGER";

	private const string BIG_BITE = "BIG_BITE";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 190, 180);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 200, 190);

	private int SnakeStrikeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 16, 13);

	private int BigBiteDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 34, 30);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/reptomancer/reptomancer.tscn";

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

	private int NumAliveDaggers()
	{
		return ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Count((Creature t) => t != ((MonsterModel)this).Creature && t.IsAlive);
	}

	private bool CanSpawn()
	{
		return NumAliveDaggers() < 4;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SPAWN_DAGGER", (Func<IReadOnlyList<Creature>, Task>)SpawnDagger, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SummonIntent() });
		MoveState val2 = new MoveState("SNAKE_STRIKE", (Func<IReadOnlyList<Creature>, Task>)SnakeStrike, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new MultiAttackIntent(SnakeStrikeDamage, 2),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val3 = new MoveState("BIG_BITE", (Func<IReadOnlyList<Creature>, Task>)BigBite, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BigBiteDamage) });
		ConditionalBranchState item = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 33)
		{
			if (!LastMove(stateMachine, "SNAKE_STRIKE"))
			{
				return "SNAKE_STRIKE";
			}
			return SelectFromReroll(rng, stateMachine, 33, 99);
		}
		if (num < 66)
		{
			if (!LastTwoMoves(stateMachine, "SPAWN_DAGGER") && CanSpawn())
			{
				return "SPAWN_DAGGER";
			}
			return "SNAKE_STRIKE";
		}
		if (!LastMove(stateMachine, "BIG_BITE"))
		{
			return "BIG_BITE";
		}
		return SelectFromReroll(rng, stateMachine, 0, 65);
	}

	private string SelectFromReroll(Rng rng, MonsterMoveStateMachine stateMachine, int min, int max)
	{
		int num = rng.NextInt(max - min + 1) + min;
		if (num < 33)
		{
			if (!LastMove(stateMachine, "SNAKE_STRIKE"))
			{
				return "SNAKE_STRIKE";
			}
			return SelectFromReroll(rng, stateMachine, 33, 99);
		}
		if (num < 66)
		{
			if (!LastTwoMoves(stateMachine, "SPAWN_DAGGER") && CanSpawn())
			{
				return "SPAWN_DAGGER";
			}
			return "SNAKE_STRIKE";
		}
		if (!LastMove(stateMachine, "BIG_BITE"))
		{
			return "BIG_BITE";
		}
		return SelectFromReroll(rng, stateMachine, 0, 65);
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

	private async Task SpawnDagger(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Summon", 0f);
		await Cmd.Wait(0.5f, false);
		HashSet<string> occupiedSlots = (from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t.IsAlive
			select t.SlotName).ToHashSet();
		int daggersSpawned = 0;
		foreach (string slot in ((MonsterModel)this).CombatState.Encounter.Slots.Where((string s) => s != "reptomancer"))
		{
			if (daggersSpawned >= 2)
			{
				break;
			}
			if (!occupiedSlots.Contains(slot))
			{
				Creature summoned = await CreatureCmd.Add<SnakeDagger>(((MonsterModel)this).CombatState, slot);
				if (summoned != null)
				{
					occupiedSlots.Add(slot);
					await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), summoned, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
					daggersSpawned++;
				}
			}
		}
	}

	private async Task SnakeStrike(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Strike", 0f);
		await Cmd.Wait(0.3f, false);
		for (int i = 0; i < 2; i++)
		{
			foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
				if (targetNode != null)
				{
					Vector2 position = targetNode.VfxSpawnPosition;
					BiteEffect effect = BiteEffect.Create(position);
					((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
					((Node2D)effect).GlobalPosition = position;
				}
			}
			await Cmd.Wait(0.1f, false);
			await DamageCmd.Attack((decimal)SnakeStrikeDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
		}
		foreach (Creature target2 in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target2, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task BigBite(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (targetNode != null)
			{
				Vector2 position = targetNode.VfxSpawnPosition;
				BiteEffect effect = BiteEffect.Create(position);
				((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
				((Node2D)effect).GlobalPosition = position;
			}
		}
		await Cmd.Wait(0.1f, false);
		await DamageCmd.Attack((decimal)BigBiteDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
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
		AnimState val3 = new AnimState("Sumon", false);
		AnimState val4 = new AnimState("Hurt", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = val;
		CreatureAnimator val5 = new CreatureAnimator(val, controller);
		val5.AddAnyState("Strike", val2, (Func<bool>)null);
		val5.AddAnyState("Summon", val3, (Func<bool>)null);
		val5.AddAnyState("Hit", val4, (Func<bool>)null);
		return val5;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
