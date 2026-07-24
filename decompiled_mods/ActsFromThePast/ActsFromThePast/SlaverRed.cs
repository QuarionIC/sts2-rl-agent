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

public sealed class SlaverRed : CustomMonsterModel
{
	private const string STAB = "STAB";

	private const string ENTANGLE = "ENTANGLE";

	private const string SCRAPE = "SCRAPE";

	private bool _usedEntangle;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 48, 46);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 52, 50);

	private int StabDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 14, 13);

	private int ScrapeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 9, 8);

	private int VulnerableAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/slaver_red/slaver_red.tscn";

	public override bool HasDeathSfx => false;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		PlayRandomDeathSfx();
	}

	private void PlayRandomDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "slaver_red_death_2" : "slaver_red_death_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("slaver_red", soundName);
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
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("STAB", (Func<IReadOnlyList<Creature>, Task>)Stab, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(StabDamage) });
		MoveState val2 = new MoveState("ENTANGLE", (Func<IReadOnlyList<Creature>, Task>)Entangle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new CardDebuffIntent() });
		MoveState val3 = new MoveState("SCRAPE", (Func<IReadOnlyList<Creature>, Task>)Scrape, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(ScrapeDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
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
		if (num >= 75 && !_usedEntangle)
		{
			return "ENTANGLE";
		}
		if (num >= 55 && _usedEntangle && !LastTwoMoves(stateMachine, "STAB"))
		{
			return "STAB";
		}
		if (!LastMove(stateMachine, "SCRAPE"))
		{
			return "SCRAPE";
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
		PlayAttackSfx();
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)StabDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "slash_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Entangle(IReadOnlyList<Creature> targets)
	{
		PlayAttackSfx();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "UseNet", 0f);
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature targetNode = ((instance2 != null) ? instance2.GetCreatureNode(target) : null);
			if (creatureNode != null && targetNode != null)
			{
				Vector2 startPos = creatureNode.VfxSpawnPosition + new Vector2(-70f, -10f);
				Vector2 endPos = targetNode.VfxSpawnPosition;
				EntangleEffect effect = EntangleEffect.Create(startPos, endPos);
				((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
			}
			await Cmd.Wait(0.2f, false);
			await PowerCmd.Apply<EntangledPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		_usedEntangle = true;
	}

	private async Task Scrape(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)ScrapeDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "slash_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)VulnerableAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private void PlayAttackSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "slaver_red_talk_2" : "slaver_red_talk_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("slaver_red", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("idleNoNet", true);
		CreatureAnimator val3 = new CreatureAnimator(val, controller);
		val3.AddAnyState("UseNet", val2, (Func<bool>)null);
		return val3;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
