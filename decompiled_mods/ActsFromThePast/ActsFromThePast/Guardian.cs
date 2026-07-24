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

public sealed class Guardian : CustomMonsterModel
{
	private const int WhirlwindDamage = 5;

	private const int WhirlwindCount = 4;

	private const int TwinSlamDamage = 8;

	private const int TwinSlamHits = 2;

	private const int DefensiveBlock = 20;

	private const int ChargeUpBlock = 9;

	private const int VentDebuffAmount = 2;

	private const int DmgThresholdIncrease = 10;

	private int _nextThreshold;

	private bool _isOpen = true;

	private MoveState _closeUpState;

	private bool _closeUpTriggered;

	private bool _pendingModeShift;

	private bool _isExecutingMove;

	private static readonly LocString _destroyDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GUARDIAN.moves.CHARGE_UP.dialog");

	private const string CLOSE_UP = "CLOSE_UP";

	private const string FIERCE_BASH = "FIERCE_BASH";

	private const string ROLL_ATTACK = "ROLL_ATTACK";

	private const string TWIN_SLAM = "TWIN_SLAM";

	private const string WHIRLWIND = "WHIRLWIND";

	private const string CHARGE_UP = "CHARGE_UP";

	private const string VENT_STEAM = "VENT_STEAM";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 250, 240);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 250, 240);

	private int FierceBashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 36, 32);

	private int RollDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 9);

	private int SharpHideThorns => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 4, 3);

	private int DmgThresholdBase => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 40, 30);

	public bool IsOpen => _isOpen;

	public bool CloseUpTriggered
	{
		get
		{
			return _closeUpTriggered;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_closeUpTriggered = value;
		}
	}

	public bool PendingModeShift
	{
		get
		{
			return _pendingModeShift;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_pendingModeShift = value;
		}
	}

	public bool IsExecutingMove => _isExecutingMove;

	protected override string VisualsPath => "res://ActsFromThePast/monsters/guardian/guardian.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_nextThreshold = DmgThresholdBase;
		await PowerCmd.Apply<ModeShiftPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)_nextThreshold, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("CHARGE_UP", (Func<IReadOnlyList<Creature>, Task>)ChargeUp, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val2 = new MoveState("FIERCE_BASH", (Func<IReadOnlyList<Creature>, Task>)FierceBash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(FierceBashDamage) });
		MoveState val3 = new MoveState("VENT_STEAM", (Func<IReadOnlyList<Creature>, Task>)VentSteam, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val4 = new MoveState("WHIRLWIND", (Func<IReadOnlyList<Creature>, Task>)Whirlwind, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(5, 4) });
		_closeUpState = new MoveState("CLOSE_UP", (Func<IReadOnlyList<Creature>, Task>)CloseUp, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val5 = new MoveState("ROLL_ATTACK", (Func<IReadOnlyList<Creature>, Task>)RollAttack, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(RollDamage) });
		MoveState val6 = new MoveState("TWIN_SLAM", (Func<IReadOnlyList<Creature>, Task>)TwinSlam, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new MultiAttackIntent(8, 2),
			(AbstractIntent)new BuffIntent()
		});
		ConditionalBranchState item = (ConditionalBranchState)(object)(val6.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("OFFENSIVE_BRANCH", SelectNextOffensiveMove))))));
		_closeUpState.FollowUpState = (MonsterState)(object)val5;
		val5.FollowUpState = (MonsterState)(object)val6;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)_closeUpState);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)val6);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextOffensiveMove(Creature owner, Rng rng, MonsterMoveStateMachine sm)
	{
		if (!_isOpen)
		{
			return "CLOSE_UP";
		}
		MonsterState? obj = ((IEnumerable<MonsterState>)sm.StateLog).LastOrDefault((Func<MonsterState, bool>)((MonsterState s) => s is MoveState));
		string text = ((obj != null) ? obj.Id : null);
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"CHARGE_UP" => "FIERCE_BASH", 
			"FIERCE_BASH" => "VENT_STEAM", 
			"VENT_STEAM" => "WHIRLWIND", 
			"TWIN_SLAM" => "WHIRLWIND", 
			"WHIRLWIND" => "CHARGE_UP", 
			_ => "CHARGE_UP", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task CheckPendingModeShift()
	{
		if (_pendingModeShift)
		{
			_pendingModeShift = false;
			CloseUpTriggered = true;
			await TransitionToDefensiveMode(setMove: false);
		}
	}

	private async Task ChargeUp(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 9m, (ValueProp)8, (CardPlay)null, false);
		AFTPModAudio.Play("guardian", "guardian_destroy");
		TalkCmd.Play(_destroyDialog, ((MonsterModel)this).Creature, (VfxColor)7, (VfxDuration)5);
		await CheckPendingModeShift();
	}

	private async Task FierceBash(IReadOnlyList<Creature> targets)
	{
		_isExecutingMove = true;
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)FierceBashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		_isExecutingMove = false;
		await CheckPendingModeShift();
	}

	private async Task VentSteam(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		await CheckPendingModeShift();
	}

	private async Task Whirlwind(IReadOnlyList<Creature> targets)
	{
		_isExecutingMove = true;
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		AFTPModAudio.Play("general", "whirlwind");
		for (int i = 0; i < 4; i++)
		{
			AFTPModAudio.Play("general", "attack_heavy");
			Creature target = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.IsAlive));
			if (target != null)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
				if (targetNode != null)
				{
					CleaveEffect cleaveVfx = CleaveEffect.Create(targetNode.VfxSpawnPosition);
					NCombatRoom instance2 = NCombatRoom.Instance;
					if (instance2 != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)cleaveVfx);
					}
				}
			}
			await Cmd.Wait(0.15f, false);
			await DamageCmd.Attack(5m).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
		}
		_isExecutingMove = false;
		await CheckPendingModeShift();
	}

	private async Task CloseUp(IReadOnlyList<Creature> targets)
	{
		await PowerCmd.Apply<SharpHidePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)SharpHideThorns, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task RollAttack(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)RollDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/punch_construct/punch_construct_attack_single", (string)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task TwinSlam(IReadOnlyList<Creature> targets)
	{
		_isExecutingMove = true;
		await TransitionToOffensiveMode();
		await DamageCmd.Attack(8m).WithHitCount(2).FromMonster((MonsterModel)(object)this)
			.WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/punch_construct/punch_construct_attack_double", (string)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		await PowerCmd.Remove<SharpHidePower>(((MonsterModel)this).Creature);
		_isExecutingMove = false;
		await CheckPendingModeShift();
	}

	public async Task TransitionToDefensiveMode(bool setMove = true)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			IntenseZoomEffect vfx = IntenseZoomEffect.Create(creatureNode.VfxSpawnPosition);
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)vfx);
			}
		}
		await PowerCmd.Remove<ModeShiftPower>(((MonsterModel)this).Creature);
		_nextThreshold += 10;
		AFTPModAudio.Play("guardian", "guardian_boss_transform");
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 20m, (ValueProp)8, (CardPlay)null, false);
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "transition", 0f);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			MegaTrackEntry trackEntry = animState.GetCurrent(0);
			if (trackEntry != null)
			{
				trackEntry.SetTimeScale(2f);
				float duration = trackEntry.GetAnimationEnd() / 2f;
				await Cmd.Wait(duration, false);
			}
			animState.SetAnimation("defensive", true, 0);
		}
		_isOpen = false;
		if (setMove)
		{
			((MonsterModel)this).SetMoveImmediate(_closeUpState, true);
		}
	}

	private async Task TransitionToOffensiveMode()
	{
		await PowerCmd.Apply<ModeShiftPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)_nextThreshold, ((MonsterModel)this).Creature, (CardModel)null, false);
		if (((MonsterModel)this).Creature.Block > 0)
		{
			await CreatureCmd.LoseBlock((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)((MonsterModel)this).Creature.Block, (Creature)null);
		}
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "idle", 0f);
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			MegaTrackEntry trackEntry = animState.GetCurrent(0);
			trackEntry.SetMixDuration(0.2f);
		}
		_isOpen = true;
		_closeUpTriggered = false;
	}

	public override async Task BeforeDeath(Creature creature)
	{
		SharpHidePower sharpHide;
		int num;
		if (creature == ((MonsterModel)this).Creature)
		{
			sharpHide = ((MonsterModel)this).Creature.GetPower<SharpHidePower>();
			if (sharpHide != null && sharpHide.AttackInProgress)
			{
				Creature attackSource = sharpHide.AttackSource;
				if (attackSource != null)
				{
					num = (attackSource.IsAlive ? 1 : 0);
					goto IL_007b;
				}
			}
			num = 0;
			goto IL_007b;
		}
		goto IL_0176;
		IL_007b:
		if (num != 0)
		{
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), sharpHide.AttackSource, (decimal)((PowerModel)sharpHide).Amount, (ValueProp)4, (CardModel)null, (CardPlay)null);
		}
		goto IL_0176;
		IL_0176:
		await _003C_003En__1(creature);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		val.set_BoundsContainer("IdleBounds");
		AnimState val2 = val;
		AnimState val3 = new AnimState("defensive", true);
		val3.set_BoundsContainer("DefensiveBounds");
		AnimState val4 = val3;
		AnimState val5 = new AnimState("transition", false);
		val5.NextState = val4;
		val2.AddBranch("transition", val5, (Func<bool>)null);
		val2.AddBranch("defensive", val4, (Func<bool>)null);
		val4.AddBranch("idle", val2, (Func<bool>)null);
		return new CreatureAnimator(val2, controller);
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
