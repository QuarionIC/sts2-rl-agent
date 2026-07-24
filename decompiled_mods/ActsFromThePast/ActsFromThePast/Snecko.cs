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
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public sealed class Snecko : CustomMonsterModel
{
	private const int VulnerableAmount = 2;

	private const int WeakAmount = 2;

	private const string GLARE = "GLARE";

	private const string BITE = "BITE";

	private const string TAIL_WHIP = "TAIL_WHIP";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 120, 114);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 125, 120);

	private int BiteDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 18, 15);

	private int TailDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 8);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/snecko/snecko.tscn";

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__0(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			AFTPModAudio.Play("snecko", "snecko_death");
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("GLARE", (Func<IReadOnlyList<Creature>, Task>)Glare, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val2 = new MoveState("BITE", (Func<IReadOnlyList<Creature>, Task>)Bite, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BiteDamage) });
		MoveState val3 = new MoveState("TAIL_WHIP", (Func<IReadOnlyList<Creature>, Task>)TailWhip, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(TailDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		RandomBranchState val4 = (RandomBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)new RandomBranchState("RANDOM"))));
		val4.AddBranch((MonsterState)(object)val2, 2, 60f);
		val4.AddBranch((MonsterState)(object)val3, (MoveRepeatType)0, 40f);
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private async Task Glare(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Glare", 0f);
		NCombatRoom combatRoom = NCombatRoom.Instance;
		NCreature sneckoNode = ((combatRoom != null) ? combatRoom.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (sneckoNode != null)
		{
			Vector2 position = sneckoNode.VfxSpawnPosition;
			IntimidateEffect effect = IntimidateEffect.Create(position);
			((Node)combatRoom.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
			((Node2D)effect).GlobalPosition = position;
		}
		AFTPModAudio.Play(((MonsterModel)this).Creature, "snecko", "snecko_glare");
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)2, (ShakeDuration)3, -1f);
		}
		await Cmd.Wait(1.5f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<ConfusedPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Bite(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Bite", 0f);
		await Cmd.Wait(0.3f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (targetNode != null)
			{
				float offsetX = (float)GD.RandRange(-50.0, 50.0);
				float offsetY = (float)GD.RandRange(-50.0, 50.0);
				Vector2 position = targetNode.VfxSpawnPosition + new Vector2(offsetX, offsetY);
				BiteEffect effect = BiteEffect.CreateChartreuse(position);
				((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
				((Node2D)effect).GlobalPosition = position;
			}
		}
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack((decimal)BiteDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
	}

	private async Task TailWhip(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack((decimal)TailDamage).FromMonster((MonsterModel)(object)this).WithAttackerAnim("TailWhip", 0.25f, (Creature)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			if (AscensionHelper.HasAscension((AscensionLevel)9))
			{
				await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
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
		val5.AddAnyState("Glare", val2, (Func<bool>)null);
		val5.AddAnyState("Bite", val3, (Func<bool>)null);
		val5.AddAnyState("TailWhip", val2, (Func<bool>)null);
		val5.AddAnyState("Hit", val4, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(0.8f);
		return val5;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
