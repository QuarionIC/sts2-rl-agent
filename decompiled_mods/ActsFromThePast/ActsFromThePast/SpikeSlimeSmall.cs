using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ActsFromThePast;

public sealed class SpikeSlimeSmall : CustomMonsterModel
{
	private const string TACKLE = "TACKLE";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 11, 10);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 15, 14);

	private int TackleDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 6, 5);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/spike_slime_small/spike_slime_small.tscn";

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)7;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/leaf_slime_s/leaf_slime_s_die", 1f);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("TACKLE", (Func<IReadOnlyList<Creature>, Task>)Tackle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(TackleDamage) });
		val.FollowUpState = (MonsterState)(object)val;
		list.Add((MonsterState)(object)val);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private async Task Tackle(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)TackleDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack", (string)null)
			.WithHitFx("vfx/vfx_slime_impact", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("hit", false);
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
