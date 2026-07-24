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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Maw : CustomMonsterModel
{
	private const int NomDamage = 5;

	private const string ROAR = "ROAR";

	private const string SLAM = "SLAM";

	private const string DROOL = "DROOL";

	private const string NOMNOMNOM_SINGLE = "NOMNOMNOM_SINGLE";

	private const string NOMNOMNOM_MULTI = "NOMNOMNOM_MULTI";

	private static readonly LocString _roarDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-MAW.moves.ROAR.dialog");

	private bool _roared = false;

	private int _turnCount = 1;

	public override int MinInitialHp => 300;

	public override int MaxInitialHp => 300;

	private int SlamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 30, 25);

	private int StrUp => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	private int TerrifyDuration => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/maw/maw.tscn";

	private bool Roared
	{
		get
		{
			return _roared;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_roared = value;
		}
	}

	private int TurnCount
	{
		get
		{
			return _turnCount;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_turnCount = value;
		}
	}

	private int NomHitCount => TurnCount / 2;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Expected O, but got Unknown
		MoveState val = new MoveState("ROAR", (Func<IReadOnlyList<Creature>, Task>)Roar, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val2 = new MoveState("SLAM", (Func<IReadOnlyList<Creature>, Task>)Slam, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SlamDamage) });
		MoveState val3 = new MoveState("DROOL", (Func<IReadOnlyList<Creature>, Task>)Drool, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val4 = new MoveState("NOMNOMNOM_SINGLE", (Func<IReadOnlyList<Creature>, Task>)NomNomNom, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(5) });
		MoveState val5 = new MoveState("NOMNOMNOM_MULTI", (Func<IReadOnlyList<Creature>, Task>)NomNomNom, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicMultiAttackIntent(() => 5, () => NomHitCount) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val5.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))))));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2,
			(MonsterState)(object)val3,
			(MonsterState)(object)val4,
			(MonsterState)(object)val5,
			(MonsterState)(object)conditionalBranchState
		}, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		TurnCount++;
		if (!Roared)
		{
			return "ROAR";
		}
		int num = rng.NextInt(100);
		bool flag = LastMove(stateMachine, "NOMNOMNOM_SINGLE") || LastMove(stateMachine, "NOMNOMNOM_MULTI");
		if (num < 50 && !flag)
		{
			return (NomHitCount <= 1) ? "NOMNOMNOM_SINGLE" : "NOMNOMNOM_MULTI";
		}
		if (!LastMove(stateMachine, "SLAM") && !flag)
		{
			return "SLAM";
		}
		return "DROOL";
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

	private async Task Roar(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("maw", "maw_death", 0f, 0.1f);
		TalkCmd.Play(_roarDialog, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		await Cmd.Wait(0.05f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)TerrifyDuration, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)TerrifyDuration, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		Roared = true;
	}

	private async Task Slam(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)SlamDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Drool(IReadOnlyList<Creature> targets)
	{
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrUp, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task NomNomNom(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		int hits = Math.Max(1, NomHitCount);
		for (int i = 0; i < hits; i++)
		{
			Creature target = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.IsAlive));
			if (target != null)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
				if (targetNode != null)
				{
					BiteEffect biteEffect = BiteEffect.Create(targetNode.VfxSpawnPosition);
					NCombatRoom instance2 = NCombatRoom.Instance;
					if (instance2 != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)biteEffect);
					}
				}
			}
			await DamageCmd.Attack(5m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
				.Execute((PlayerChoiceContext)null);
		}
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__0(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			AFTPModAudio.Play("maw", "maw_death");
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		return new CreatureAnimator(val, controller);
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
