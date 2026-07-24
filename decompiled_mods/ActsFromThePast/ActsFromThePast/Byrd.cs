using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Byrd : CustomMonsterModel
{
	private static readonly LocString _cawLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-BYRD.moves.CAW.speakLine");

	private bool _isFlying = true;

	private const int HeadbuttDamage = 3;

	private const int CawStrength = 1;

	private const string FlyingVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_flying.tscn";

	private const string GroundedVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_grounded.tscn";

	private const string PECK = "PECK";

	private const string SWOOP = "SWOOP";

	private const string CAW = "CAW";

	private const string GO_AIRBORNE = "GO_AIRBORNE";

	private const string HEADBUTT = "HEADBUTT";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 26, 25);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 33, 31);

	private int PeckDamage => 1;

	private int PeckCount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 6, 5);

	private int SwoopDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 14, 12);

	private int FlightAmountPerPlayer => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 4, 3);

	private int FlightAmount
	{
		get
		{
			int num = ((MonsterModel)this).Creature.CombatState.Players.Count - 1;
			return FlightAmountPerPlayer + num * 2;
		}
	}

	protected override string VisualsPath => "res://ActsFromThePast/monsters/byrd/byrd_flying.tscn";

	public override IEnumerable<string> AssetPaths => new List<string>(((MonsterModel)this).AssetPaths) { "res://ActsFromThePast/monsters/byrd/byrd_grounded.tscn" };

	private bool IsFlying
	{
		get
		{
			return _isFlying;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_isFlying = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<FlightPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)FlightAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	public async Task OnFlightBroken()
	{
		IsFlying = false;
		await ByrdFallAnimation.Play(((MonsterModel)this).Creature, 100f);
		SwapVisuals("res://ActsFromThePast/monsters/byrd/byrd_grounded.tscn");
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			NCreatureVisuals visuals = creatureNode.Visuals;
			Vector2 baseScale = ((Node2D)visuals).Scale;
			Tween squashTween = ((Node)creatureNode).CreateTween();
			squashTween.TweenProperty((GodotObject)(object)visuals, NodePath.op_Implicit("scale"), Variant.op_Implicit(new Vector2(baseScale.X * 1.2f, baseScale.Y * 0.8f)), 0.07500000298023224);
			squashTween.TweenProperty((GodotObject)(object)visuals, NodePath.op_Implicit("scale"), Variant.op_Implicit(baseScale), 0.07500000298023224);
			await ((GodotObject)creatureNode).ToSignal((GodotObject)(object)squashTween, SignalName.Finished);
		}
		await CreatureCmd.Stun(((MonsterModel)this).Creature, "HEADBUTT");
	}

	private void SwapVisuals(string newVisualsPath)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (val != null)
		{
			NCreatureVisuals visuals = val.Visuals;
			NCreatureVisuals val2 = PreloadManager.Cache.GetScene(newVisualsPath).Instantiate<NCreatureVisuals>((GenEditState)0);
			typeof(NCreature).GetProperty("Visuals")?.SetValue(val, val2);
			GodotTreeExtensions.AddChildSafely((Node)(object)val, (Node)(object)val2);
			((Node)val).MoveChild((Node)(object)val2, 0);
			((Node2D)val2).Position = Vector2.Zero;
			if (val2.HasSpineAnimation)
			{
				CreatureAnimator value = GenerateAnimatorForState(val2.SpineBody);
				typeof(NCreature).GetField("_spineAnimator", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(val, value);
			}
			typeof(NCreature).GetMethod("UpdateBounds", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { typeof(Node) }, null)?.Invoke(val, new object[1] { val2 });
			((Node)visuals).QueueFree();
		}
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			AFTPModAudio.Play("byrd", "byrd_death");
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("PECK", (Func<IReadOnlyList<Creature>, Task>)Peck, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(PeckDamage, PeckCount) });
		MoveState val2 = new MoveState("SWOOP", (Func<IReadOnlyList<Creature>, Task>)Swoop, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SwoopDamage) });
		MoveState val3 = new MoveState("CAW", (Func<IReadOnlyList<Creature>, Task>)Caw, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val4 = new MoveState("GO_AIRBORNE", (Func<IReadOnlyList<Creature>, Task>)GoAirborne, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val5 = new MoveState("HEADBUTT", (Func<IReadOnlyList<Creature>, Task>)Headbutt, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(3) });
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("FLYING_BRANCH", SelectFlyingMove);
		ConditionalBranchState conditionalBranchState2 = new ConditionalBranchState("FIRST_MOVE_BRANCH", SelectFirstMove);
		val.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val2.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val3.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val5.FollowUpState = (MonsterState)(object)val4;
		val4.FollowUpState = (MonsterState)(object)conditionalBranchState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)conditionalBranchState);
		list.Add((MonsterState)(object)conditionalBranchState2);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState2);
	}

	private string SelectFirstMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		return (rng.NextFloat(1f) < 0.375f) ? "CAW" : "PECK";
	}

	private string SelectFlyingMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 50)
		{
			if (LastTwoMoves(stateMachine, "PECK"))
			{
				return (rng.NextFloat(1f) < 0.4f) ? "SWOOP" : "CAW";
			}
			return "PECK";
		}
		if (num < 70)
		{
			if (LastMove(stateMachine, "SWOOP"))
			{
				return (rng.NextFloat(1f) < 0.375f) ? "CAW" : "PECK";
			}
			return "SWOOP";
		}
		if (LastMove(stateMachine, "CAW"))
		{
			return (rng.NextFloat(1f) < 0.2857f) ? "SWOOP" : "PECK";
		}
		return "CAW";
	}

	private async Task Peck(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		for (int i = 0; i < PeckCount; i++)
		{
			PlayRandomBirdSfx();
			await DamageCmd.Attack((decimal)PeckDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task Swoop(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)SwoopDamage).FromMonster((MonsterModel)(object)this).WithHitFx((string)null, (string)null, "slash_attack.mp3")
			.WithHitVfxNode((Func<Creature, Node2D>)delegate(Creature target)
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_005d: Unknown result type (might be due to invalid IL or missing references)
				//IL_007a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0071: Unknown result type (might be due to invalid IL or missing references)
				Node2D val = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_scratch")).Instantiate<Node2D>((GenEditState)0);
				val.Scale = new Vector2(-1f, 1f);
				NCombatRoom instance = NCombatRoom.Instance;
				Vector2? obj;
				if (instance == null)
				{
					obj = null;
				}
				else
				{
					NCreature creatureNode = instance.GetCreatureNode(target);
					obj = ((creatureNode != null) ? new Vector2?(creatureNode.VfxSpawnPosition) : ((Vector2?)null));
				}
				val.GlobalPosition = (Vector2)(((_003F?)obj) ?? Vector2.Zero);
				return val;
			})
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Caw(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("byrd", "byrd_death");
		TalkCmd.Play(_cawLine, ((MonsterModel)this).Creature, (VfxColor)9, (VfxDuration)2);
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task GoAirborne(IReadOnlyList<Creature> targets)
	{
		IsFlying = true;
		SwapVisuals("res://ActsFromThePast/monsters/byrd/byrd_flying.tscn");
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			NCreatureVisuals visuals = creatureNode.Visuals;
			((Node2D)visuals).Position = new Vector2(((Node2D)visuals).Position.X, ((Node2D)visuals).Position.Y + 80f);
			await RiseAnimation.Play(((MonsterModel)this).Creature, 100f);
			Tween settleTween = ((Node)creatureNode).CreateTween();
			settleTween.TweenProperty((GodotObject)(object)visuals, NodePath.op_Implicit("position:y"), Variant.op_Implicit(0f), 0.20000000298023224).SetEase((EaseType)2).SetTrans((TransitionType)1);
			await ((GodotObject)creatureNode).ToSignal((GodotObject)(object)settleTween, SignalName.Finished);
		}
		AFTPModAudio.Play("byrd", "flight");
		await PowerCmd.Apply<FlightPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)FlightAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Headbutt(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack(3m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private void PlayRandomBirdSfx()
	{
		int value = Rng.Chaotic.NextInt(6) + 1;
		AFTPModAudio.Play("byrd", $"byrd_talk_{value}");
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

	private CreatureAnimator GenerateAnimatorForState(MegaSprite controller)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		CreatureAnimator val2;
		if (IsFlying)
		{
			AnimState val = new AnimState("idle_flap", true);
			val2 = new CreatureAnimator(val, controller);
		}
		else
		{
			AnimState val3 = new AnimState("idle", true);
			AnimState val4 = new AnimState("head_lift", false);
			val4.NextState = val3;
			val2 = new CreatureAnimator(val3, controller);
			val2.AddAnyState("Attack", val4, (Func<bool>)null);
		}
		MegaAnimationState animationState = controller.GetAnimationState();
		MegaTrackEntry current = animationState.GetCurrent(0);
		current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
		animationState.Update(0f);
		animationState.Apply(controller.GetSkeleton());
		return val2;
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return GenerateAnimatorForState(controller);
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
