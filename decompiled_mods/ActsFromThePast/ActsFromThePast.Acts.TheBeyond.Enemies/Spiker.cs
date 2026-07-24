using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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

public sealed class Spiker : CustomMonsterModel
{
	private const int BuffAmount = 2;

	private const string ATTACK = "ATTACK";

	private const string BUFF_THORNS = "BUFF_THORNS";

	private int _thornsCount;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 44, 42);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 60, 56);

	private int AttackDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 7);

	private int StartingThorns => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 7, 4);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/spiker/spiker.tscn";

	private int ThornsCount
	{
		get
		{
			return _thornsCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_thornsCount = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_thornsCount = 0;
		await PowerCmd.Apply<ThornsPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StartingThorns, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		MoveState val = new MoveState("ATTACK", (Func<IReadOnlyList<Creature>, Task>)Attack, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(AttackDamage) });
		MoveState val2 = new MoveState("BUFF_THORNS", (Func<IReadOnlyList<Creature>, Task>)BuffThorns, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (ThornsCount > 5)
		{
			return "ATTACK";
		}
		int num = rng.NextInt(100);
		if (num < 50 && !LastMove(stateMachine, "ATTACK"))
		{
			return "ATTACK";
		}
		return "BUFF_THORNS";
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

	private async Task Attack(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "SpikerPounce", 0f);
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			MegaTrackEntry trackEntry = animState.GetCurrent(0);
			if (trackEntry != null)
			{
				trackEntry.SetTimeScale(3f);
			}
		}
		await Cmd.Wait(0.25f, false);
		await DamageCmd.Attack((decimal)AttackDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task BuffThorns(IReadOnlyList<Creature> targets)
	{
		ThornsCount++;
		await PowerCmd.Apply<ThornsPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
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
		AnimState val2 = new AnimState("attack", false);
		AnimState val3 = new AnimState("damaged", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("SpikerPounce", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
