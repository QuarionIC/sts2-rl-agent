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

public sealed class BookOfStabbing : CustomMonsterModel
{
	private const string STAB = "STAB";

	private const string BIG_STAB = "BIG_STAB";

	private int _stabCount;

	private static readonly string[] StabVfxPaths = new string[4] { "vfx/slash/vfx_slash_core", "vfx/vfx_dramatic_stab", "vfx/vfx_attack_slash", "vfx/vfx_big_slash" };

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 168, 160);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 172, 164);

	private int StabDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 7, 6);

	private int BigStabDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 24, 21);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/book_of_stabbing/book_of_stabbing.tscn";

	private int StabCount
	{
		get
		{
			return _stabCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_stabCount = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_stabCount = 1;
		await PowerCmd.Apply<PainfulStabsPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		AFTPModAudio.Play("book_of_stabbing", "book_of_stabbing_death");
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("STAB", (Func<IReadOnlyList<Creature>, Task>)Stab, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicMultiAttackIntent(() => StabDamage, () => StabCount) });
		MoveState val2 = new MoveState("BIG_STAB", (Func<IReadOnlyList<Creature>, Task>)BigStab, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BigStabDamage) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 15)
		{
			if (LastMove(stateMachine, "BIG_STAB"))
			{
				StabCount++;
				return "STAB";
			}
			StabCount++;
			return "BIG_STAB";
		}
		if (LastTwoMoves(stateMachine, "STAB"))
		{
			StabCount++;
			return "BIG_STAB";
		}
		StabCount++;
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

	private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 1].Id == moveId && stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task Stab(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack((decimal)StabDamage).WithHitCount(StabCount).FromMonster((MonsterModel)(object)this)
			.WithAttackerAnim("Stab", 0.5f, (Creature)null)
			.OnlyPlayAnimOnce()
			.BeforeDamage((Func<Task>)async delegate
			{
				PlayStabSfx();
			})
			.WithHitVfxNode((Func<Creature, Node2D>)((Creature target) => CreateRandomStabVfx(target)))
			.Execute((PlayerChoiceContext)null);
	}

	private static Node2D? CreateRandomStabVfx(Creature target)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val == null)
		{
			return null;
		}
		string text = StabVfxPaths[Rng.Chaotic.NextInt(StabVfxPaths.Length)];
		Node2D val2 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(text)).Instantiate<Node2D>((GenEditState)0);
		val2.Scale = new Vector2(-1f, 1f);
		val2.GlobalPosition = val.VfxSpawnPosition;
		return val2;
	}

	private void PlayStabSfx()
	{
		int value = Rng.Chaotic.NextInt(4) + 1;
		string soundName = $"book_of_stabbing_attack_{value}";
		AFTPModAudio.Play("book_of_stabbing", soundName);
	}

	private async Task BigStab(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack((decimal)BigStabDamage).FromMonster((MonsterModel)(object)this).WithAttackerAnim("BigStab", 0.5f, (Creature)null)
			.WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.BeforeDamage((Func<Task>)async delegate
			{
				PlayStabSfx();
			})
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
		AnimState val3 = new AnimState("Attack_2", false);
		AnimState val4 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = val;
		CreatureAnimator val5 = new CreatureAnimator(val, controller);
		val5.AddAnyState("Stab", val2, (Func<bool>)null);
		val5.AddAnyState("BigStab", val3, (Func<bool>)null);
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
