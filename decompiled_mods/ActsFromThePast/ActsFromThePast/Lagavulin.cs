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
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Lagavulin : CustomMonsterModel
{
	public const string SLEEP = "SLEEP";

	public const string ATTACK = "ATTACK";

	public const string DEBUFF = "DEBUFF";

	private static readonly LocString _sleepLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.SLEEP_1");

	private static readonly LocString _sleepLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.SLEEP_2");

	private static readonly LocString _wakeLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.WAKE");

	private const int MetalAmount = 8;

	private const int AsleepTurns = 3;

	private bool _isAwake;

	private int _debuffTurnCount;

	private int _sleepTurnCount;

	private NSleepingVfx? _sleepingVfx;

	private bool _startsAwake;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 112, 109);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 115, 111);

	private int AttackDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 20, 18);

	private int DebuffAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, -2, -1);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/lagavulin/lagavulin.tscn";

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)2;

	public bool IsAwake
	{
		get
		{
			return _isAwake;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_isAwake = value;
		}
	}

	public int DebuffTurnCount
	{
		get
		{
			return _debuffTurnCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_debuffTurnCount = value;
		}
	}

	public int SleepTurnCount
	{
		get
		{
			return _sleepTurnCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_sleepTurnCount = value;
		}
	}

	public bool StartsAwake
	{
		get
		{
			return _startsAwake;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_startsAwake = value;
		}
	}

	private NSleepingVfx? SleepingVfx
	{
		get
		{
			return _sleepingVfx;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_sleepingVfx = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		if (StartsAwake)
		{
			IsAwake = true;
			ApplyAwakeBounds();
		}
		else
		{
			await PowerCmd.Apply<MetallicizePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 8m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<AsleepLagavulinPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
			NCombatRoom instance = NCombatRoom.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				NCreature creatureNode = instance.GetCreatureNode(((MonsterModel)this).Creature);
				obj = ((creatureNode != null) ? creatureNode.GetSpecialNode<Marker2D>("%SleepVfxPos") : null);
			}
			Marker2D specialNode = (Marker2D)obj;
			if (specialNode != null)
			{
				SleepingVfx = NSleepingVfx.Create(((Node2D)specialNode).GlobalPosition, true);
				GodotTreeExtensions.AddChildSafely((Node)(object)specialNode, (Node)(object)SleepingVfx);
				((Node2D)SleepingVfx).Position = Vector2.Zero;
			}
		}
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		StopSleepingVfx();
	}

	public void StopSleepingVfx()
	{
		NSleepingVfx? sleepingVfx = SleepingVfx;
		if (sleepingVfx != null)
		{
			sleepingVfx.Stop();
		}
		SleepingVfx = null;
	}

	public async Task WakeUpFromDamage()
	{
		TalkCmd.Play(_wakeLine, ((MonsterModel)this).Creature, (VfxColor)5, (VfxDuration)4);
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Wake", 0.6f);
		StopSleepingVfx();
		IsAwake = true;
		ApplyAwakeBounds();
		await CreatureCmd.Stun(((MonsterModel)this).Creature, (Func<IReadOnlyList<Creature>, Task>)StunnedMove, "ATTACK");
		NRunMusicController instance = NRunMusicController.Instance;
		if (instance != null)
		{
			instance.UpdateTrack();
		}
	}

	public async Task WakeUpNaturally()
	{
		TalkCmd.Play(_wakeLine, ((MonsterModel)this).Creature, (VfxColor)5, (VfxDuration)4);
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Wake", 0.6f);
		StopSleepingVfx();
		IsAwake = true;
		ApplyAwakeBounds();
		NRunMusicController instance = NRunMusicController.Instance;
		if (instance != null)
		{
			instance.UpdateTrack();
		}
	}

	private void ApplyAwakeBounds()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (val != null)
		{
			Node node = ((Node)val).GetNode<Node>(NodePath.op_Implicit("Lagavulin"));
			Control nodeOrNull = node.GetNodeOrNull<Control>(NodePath.op_Implicit("Bounds"));
			Control nodeOrNull2 = node.GetNodeOrNull<Control>(NodePath.op_Implicit("AwakeBounds/Bounds"));
			if (nodeOrNull != null && nodeOrNull2 != null)
			{
				nodeOrNull.Position = nodeOrNull2.Position;
				nodeOrNull.Size = nodeOrNull2.Size;
			}
			Marker2D nodeOrNull3 = node.GetNodeOrNull<Marker2D>(NodePath.op_Implicit("CenterPos"));
			Marker2D nodeOrNull4 = node.GetNodeOrNull<Marker2D>(NodePath.op_Implicit("AwakeBounds/CenterPos"));
			if (nodeOrNull3 != null && nodeOrNull4 != null)
			{
				((Node2D)nodeOrNull3).Position = ((Node2D)nodeOrNull4).Position;
			}
			Marker2D nodeOrNull5 = node.GetNodeOrNull<Marker2D>(NodePath.op_Implicit("IntentPos"));
			Marker2D nodeOrNull6 = node.GetNodeOrNull<Marker2D>(NodePath.op_Implicit("AwakeBounds/IntentPos"));
			if (nodeOrNull5 != null && nodeOrNull6 != null)
			{
				((Node2D)nodeOrNull5).Position = ((Node2D)nodeOrNull6).Position;
			}
			Control nodeOrNull7 = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("Hitbox"));
			if (nodeOrNull7 != null && nodeOrNull2 != null)
			{
				nodeOrNull7.Position = nodeOrNull2.Position;
				nodeOrNull7.Size = nodeOrNull2.Size;
			}
		}
	}

	public async Task StunnedMove(IReadOnlyList<Creature> targets)
	{
		await Task.CompletedTask;
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
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SLEEP", (Func<IReadOnlyList<Creature>, Task>)SleepMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SleepIntent() });
		MoveState val2 = new MoveState("ATTACK", (Func<IReadOnlyList<Creature>, Task>)AttackMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(AttackDamage) });
		MoveState val3 = new MoveState("DEBUFF", (Func<IReadOnlyList<Creature>, Task>)DebuffMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MAIN_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (!IsAwake && ((MonsterModel)this).Creature.HasPower<AsleepLagavulinPower>())
		{
			return "SLEEP";
		}
		if (StartsAwake && stateMachine.StateLog.Count == 0)
		{
			return "DEBUFF";
		}
		if (DebuffTurnCount >= 2)
		{
			return "DEBUFF";
		}
		if (LastTwoMoves(stateMachine, "ATTACK"))
		{
			return "DEBUFF";
		}
		return "ATTACK";
	}

	private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		int result;
		if (stateLog[stateLog.Count - 1].Id == moveId)
		{
			result = ((stateLog[stateLog.Count - 2].Id == moveId) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private Task SleepMove(IReadOnlyList<Creature> targets)
	{
		SleepTurnCount++;
		switch (SleepTurnCount)
		{
		case 1:
			TalkCmd.Play(_sleepLine1, ((MonsterModel)this).Creature, (VfxColor)5, (VfxDuration)4);
			break;
		case 2:
			TalkCmd.Play(_sleepLine2, ((MonsterModel)this).Creature, (VfxColor)5, (VfxDuration)4);
			break;
		}
		return Task.CompletedTask;
	}

	private async Task AttackMove(IReadOnlyList<Creature> targets)
	{
		DebuffTurnCount++;
		await DamageCmd.Attack((decimal)AttackDamage).FromMonster((MonsterModel)(object)this).WithAttackerAnim("Attack", 0.3f, (Creature)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task DebuffMove(IReadOnlyList<Creature> targets)
	{
		DebuffTurnCount = 0;
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Debuff", 0f);
		await Cmd.Wait(0.3f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)DebuffAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)DebuffAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		AnimState val = new AnimState("Idle_1", true);
		val.set_BoundsContainer("SleepingBounds");
		AnimState val2 = val;
		AnimState val3 = new AnimState("Idle_1", true);
		AnimState val4 = new AnimState("Coming_out", false);
		AnimState val5 = new AnimState("Idle_2", true);
		val5.set_BoundsContainer("AwakeBounds");
		AnimState val6 = val5;
		AnimState val7 = new AnimState("Attack", false);
		AnimState val8 = new AnimState("Debuff", false);
		AnimState val9 = new AnimState("Hit", false);
		val3.NextState = val2;
		val4.NextState = val6;
		val7.NextState = val6;
		val8.NextState = val6;
		val9.NextState = val6;
		AnimState val10 = (StartsAwake ? val6 : val2);
		CreatureAnimator val11 = new CreatureAnimator(val10, controller);
		val11.AddAnyState("Sleep", val2, (Func<bool>)null);
		val11.AddAnyState("Wake", val4, (Func<bool>)(() => !IsAwake));
		val11.AddAnyState("Idle_2", val6, (Func<bool>)null);
		val11.AddAnyState("Attack", val7, (Func<bool>)null);
		val11.AddAnyState("Debuff", val8, (Func<bool>)null);
		val11.AddAnyState("Hit", val9, (Func<bool>)(() => IsAwake));
		val11.AddAnyState("Hit", val3, (Func<bool>)(() => !IsAwake));
		return val11;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
