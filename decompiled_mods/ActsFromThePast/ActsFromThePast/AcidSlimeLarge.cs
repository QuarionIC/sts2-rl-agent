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
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class AcidSlimeLarge : CustomMonsterModel
{
	private int? _overrideHp;

	private const int WeakTurns = 2;

	private const int SlimedCount = 2;

	private const string CORROSIVE_SPIT = "CORROSIVE_SPIT";

	private const string TACKLE = "TACKLE";

	private const string LICK = "LICK";

	private const string SPLIT = "SPLIT";

	private bool _splitTriggered;

	private MoveState _splitState;

	public int? OverrideHp
	{
		get
		{
			return _overrideHp;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_overrideHp = value;
		}
	}

	public override int MinInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension((AscensionLevel)8, 68, 65);

	public override int MaxInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension((AscensionLevel)8, 72, 69);

	private int CorrosiveSpitDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 11);

	private int TackleDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 18, 16);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/acid_slime_large/acid_slime_large.tscn";

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)7;

	public bool SplitTriggered
	{
		get
		{
			return _splitTriggered;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_splitTriggered = value;
		}
	}

	public MoveState SplitState => _splitState;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<SplitPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("CORROSIVE_SPIT", (Func<IReadOnlyList<Creature>, Task>)CorrosiveSpit, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(CorrosiveSpitDamage),
			(AbstractIntent)new StatusIntent(2)
		});
		MoveState val2 = new MoveState("TACKLE", (Func<IReadOnlyList<Creature>, Task>)Tackle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(TackleDamage) });
		MoveState val3 = new MoveState("LICK", (Func<IReadOnlyList<Creature>, Task>)Lick, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		_splitState = new MoveState("SPLIT", (Func<IReadOnlyList<Creature>, Task>)Split, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		_splitState.FollowUpState = (MonsterState)(object)_splitState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)_splitState);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (SplitTriggered)
		{
			return "SPLIT";
		}
		int num = rng.NextInt(100);
		if (num < 40)
		{
			if (LastTwoMoves(stateMachine, "CORROSIVE_SPIT"))
			{
				return (rng.NextFloat(1f) < 0.6f) ? "TACKLE" : "LICK";
			}
			return "CORROSIVE_SPIT";
		}
		if (num < 70)
		{
			if (LastTwoMoves(stateMachine, "TACKLE"))
			{
				return (rng.NextFloat(1f) < 0.6f) ? "CORROSIVE_SPIT" : "LICK";
			}
			return "TACKLE";
		}
		if (LastMove(stateMachine, "LICK"))
		{
			return (rng.NextFloat(1f) < 0.4f) ? "CORROSIVE_SPIT" : "TACKLE";
		}
		return "LICK";
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

	private async Task CorrosiveSpit(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)CorrosiveSpitDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack", (string)null)
			.WithHitFx("vfx/vfx_slime_impact", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		try
		{
			ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
			await CardPileCmd.AddToCombatAndPreview<Slimed>((IEnumerable<Creature>)targets, (PileType)3, 2, (Player)null, (CardPilePosition)1);
		}
		finally
		{
			ClassicSlimedTracker.CreatingClassicSlimed = false;
		}
	}

	private async Task Tackle(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)TackleDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack", (string)null)
			.WithHitFx("vfx/vfx_slime_impact", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Lick(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Split(IReadOnlyList<Creature> targets)
	{
		int currentHp = ((MonsterModel)this).Creature.CurrentHp;
		ICombatState combatState = ((MonsterModel)this).Creature.CombatState;
		NCombatRoom instance = NCombatRoom.Instance;
		Vector2? obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NCreature creatureNode = instance.GetCreatureNode(((MonsterModel)this).Creature);
			obj = ((creatureNode != null) ? new Vector2?(((Control)creatureNode).Position) : ((Vector2?)null));
		}
		Vector2 originalPosition = (Vector2)(((_003F?)obj) ?? Vector2.Zero);
		ShakeAnimation.Play(((MonsterModel)this).Creature, 1f, 3f);
		await Cmd.Wait(1f, false);
		AFTPModAudio.Play("general", "slime_split");
		await CreatureCmd.Kill(((MonsterModel)this).Creature, false);
		HashSet<string> occupiedSlots = (from t in combatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t.IsAlive
			select t.SlotName).ToHashSet();
		string slot1 = combatState.Encounter.Slots?.FirstOrDefault((string s) => s.StartsWith("acid_med") && !occupiedSlots.Contains(s));
		string slot2 = null;
		if (slot1 != null)
		{
			occupiedSlots.Add(slot1);
			slot2 = combatState.Encounter.Slots?.FirstOrDefault((string s) => s.StartsWith("acid_med") && !occupiedSlots.Contains(s));
		}
		bool useSlots = slot1 != null && slot2 != null;
		Queue<Vector2> positionQueue = null;
		NCombatRoom instance2 = NCombatRoom.Instance;
		Control enemyContainer = ((instance2 != null) ? ((Node)instance2).GetNode<Control>(NodePath.op_Implicit("%EnemyContainer")) : null);
		Callable? callable = null;
		if (!useSlots)
		{
			positionQueue = new Queue<Vector2>();
			callable = Callable.From<Node>((Action<Node>)OnChildEntered);
			if (enemyContainer != null)
			{
				((GodotObject)enemyContainer).Connect(SignalName.ChildEnteredTree, callable.Value, 0u);
			}
			positionQueue.Enqueue(originalPosition + new Vector2(-134f, Rng.Chaotic.NextFloat(1f) * 8f - 4f));
		}
		AcidSlimeMedium slime1 = (AcidSlimeMedium)(object)((MonsterModel)ModelDb.Monster<AcidSlimeMedium>()).ToMutable();
		Creature creature1 = await CreatureCmd.Add((MonsterModel)(object)slime1, combatState, (CombatSide)2, slot1);
		await CreatureCmd.SetMaxHp(creature1, (decimal)currentHp);
		await CreatureCmd.Heal(creature1, (decimal)currentHp, true);
		if (!useSlots)
		{
			positionQueue.Enqueue(originalPosition + new Vector2(134f, Rng.Chaotic.NextFloat(1f) * 8f - 4f));
		}
		AcidSlimeMedium slime2 = (AcidSlimeMedium)(object)((MonsterModel)ModelDb.Monster<AcidSlimeMedium>()).ToMutable();
		Creature creature2 = await CreatureCmd.Add((MonsterModel)(object)slime2, combatState, (CombatSide)2, slot2);
		await CreatureCmd.SetMaxHp(creature2, (decimal)currentHp);
		await CreatureCmd.Heal(creature2, (decimal)currentHp, true);
		if (!useSlots && callable.HasValue)
		{
			if (enemyContainer != null)
			{
				((GodotObject)enemyContainer).Disconnect(SignalName.ChildEnteredTree, callable.Value);
			}
		}
		void OnChildEntered(Node child)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			NCreature val = (NCreature)(object)((child is NCreature) ? child : null);
			if (val != null && positionQueue.Count > 0)
			{
				((Control)val).Position = positionQueue.Dequeue();
			}
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("damage", false);
		val2.NextState = val;
		CreatureAnimator val3 = new CreatureAnimator(val, controller);
		val3.AddAnyState("Hit", val2, (Func<bool>)null);
		return val3;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
