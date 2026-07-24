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
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class BronzeAutomaton : CustomMonsterModel
{
	private const int FlailHits = 2;

	private const int ArtifactAmount = 3;

	private const string SPAWN_ORBS = "SPAWN_ORBS";

	private const string BOOST = "BOOST";

	private const string FLAIL = "FLAIL";

	private const string HYPER_BEAM = "HYPER_BEAM";

	private int _numTurns;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 320, 300);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 320, 300);

	private int FlailDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 8, 7);

	private int BeamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 50, 45);

	private int StrAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 4, 3);

	private int BlockAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 12, 9);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/bronze_automaton/bronze_automaton.tscn";

	private int NumTurns
	{
		get
		{
			return _numTurns;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_numTurns = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_numTurns = 0;
		await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)4, (ShakeDuration)3, -1f);
		}
		List<Creature> livingMinions = (from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t).ToList();
		foreach (Creature minion in livingMinions)
		{
			await CreatureCmd.Kill(minion, false);
		}
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
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SPAWN_ORBS", (Func<IReadOnlyList<Creature>, Task>)SpawnOrbs, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SummonIntent() });
		MoveState val2 = new MoveState("BOOST", (Func<IReadOnlyList<Creature>, Task>)Boost, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val3 = new MoveState("FLAIL", (Func<IReadOnlyList<Creature>, Task>)Flail, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(FlailDamage, 2) });
		MoveState val4 = new MoveState("HYPER_BEAM", (Func<IReadOnlyList<Creature>, Task>)HyperBeam, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BeamDamage) });
		ConditionalBranchState item = (ConditionalBranchState)(object)(val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
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

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (NumTurns == 4)
		{
			NumTurns = 0;
			return "HYPER_BEAM";
		}
		if (LastMove(stateMachine, "HYPER_BEAM"))
		{
			return "BOOST";
		}
		NumTurns++;
		if (!LastMove(stateMachine, "BOOST") && !LastMove(stateMachine, "SPAWN_ORBS"))
		{
			return "BOOST";
		}
		return "FLAIL";
	}

	private async Task SpawnOrbs(IReadOnlyList<Creature> targets)
	{
		List<string> slots = ((MonsterModel)this).CombatState.Encounter.Slots.Where((string s) => s.StartsWith("orb")).ToList();
		int index = 0;
		List<Task> spawnTasks = new List<Task>();
		foreach (string slot in slots)
		{
			BronzeOrb orb = (BronzeOrb)(object)((MonsterModel)ModelDb.Monster<BronzeOrb>()).ToMutable();
			orb.BobIndex = index;
			orb.SpawnAnimPending = true;
			Creature summoned = await CreatureCmd.Add((MonsterModel)(object)orb, ((MonsterModel)this).CombatState, (CombatSide)2, slot);
			await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), summoned, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			spawnTasks.Add(BronzeOrbSpawnAnimation.Play(summoned));
			index++;
		}
		if (spawnTasks.Count > 0)
		{
			await Task.WhenAll(spawnTasks);
		}
	}

	private async Task Boost(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)BlockAmount, (ValueProp)8, (CardPlay)null, false);
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Flail(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		for (int i = 0; i < 2; i++)
		{
			await DamageCmd.Attack((decimal)FlailDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task HyperBeam(IReadOnlyList<Creature> targets)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature automatonNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		Creature target = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.IsAlive));
		object obj;
		if (target == null)
		{
			obj = null;
		}
		else
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			obj = ((instance2 != null) ? instance2.GetCreatureNode(target) : null);
		}
		NCreature targetNode = (NCreature)obj;
		if (automatonNode != null && targetNode != null)
		{
			Vector2 sourcePos = automatonNode.VfxSpawnPosition;
			Vector2 targetPos = targetNode.VfxSpawnPosition;
			NHyperbeamVfx beam = NHyperbeamVfx.Create(sourcePos, targetPos);
			if (beam != null)
			{
				NCombatRoom instance3 = NCombatRoom.Instance;
				if (instance3 != null)
				{
					Control combatVfxContainer = instance3.CombatVfxContainer;
					if (combatVfxContainer != null)
					{
						((Node)combatVfxContainer).AddChild((Node)(object)beam, false, (InternalMode)0);
					}
				}
			}
			NHyperbeamImpactVfx impact = NHyperbeamImpactVfx.Create(sourcePos, targetPos);
			if (impact != null)
			{
				NCombatRoom instance4 = NCombatRoom.Instance;
				if (instance4 != null)
				{
					Control combatVfxContainer2 = instance4.CombatVfxContainer;
					if (combatVfxContainer2 != null)
					{
						((Node)combatVfxContainer2).AddChild((Node)(object)impact, false, (InternalMode)0);
					}
				}
			}
			await Cmd.Wait(NHyperbeamVfx.hyperbeamAnticipationDuration + NHyperbeamVfx.hyperbeamLaserDuration, false);
		}
		await DamageCmd.Attack((decimal)BeamDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
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
