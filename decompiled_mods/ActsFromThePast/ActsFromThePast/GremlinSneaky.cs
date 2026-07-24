using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinSneaky : CustomMonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 11, 10);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 15, 14);

	private int PunctureDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 9);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_sneaky/gremlin_sneaky.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		MoveState val = new MoveState("PUNCTURE", (Func<IReadOnlyList<Creature>, Task>)Puncture, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(PunctureDamage) });
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { (MonsterState)(object)val }, (MonsterState)(object)val);
	}

	private async Task Puncture(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)PunctureDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
		GremlinLeaderHelper.SubscribeToLeaderDeath(((MonsterModel)this).Creature, (CombatState)((MonsterModel)this).CombatState);
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		PlayRandomDeathSfx();
	}

	private void PlayRandomDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "gremlin_sneaky_death_1", 
			1 => "gremlin_sneaky_death_2", 
			_ => "gremlin_sneaky_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_sneaky", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("animation", true);
		CreatureAnimator result = new CreatureAnimator(val, controller);
		MegaAnimationState animationState = controller.GetAnimationState();
		MegaTrackEntry current = animationState.GetCurrent(0);
		current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
		animationState.Update(0f);
		animationState.Apply(controller.GetSkeleton());
		return result;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
