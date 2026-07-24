using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinFat : CustomMonsterModel
{
	private const int WeakAmount = 1;

	private const int FrailAmount = 1;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 14, 13);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 18, 17);

	private int SmashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 4);

	private bool AppliesFrail => AscensionHelper.HasAscension((AscensionLevel)9);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_fat/gremlin_fat.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		MoveState val = new MoveState("SMASH", (Func<IReadOnlyList<Creature>, Task>)Smash, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(SmashDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { (MonsterState)(object)val }, (MonsterState)(object)val);
	}

	private async Task Smash(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)SmashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			if (AppliesFrail)
			{
				await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
		}
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
			0 => "gremlin_fat_death_1", 
			1 => "gremlin_fat_death_2", 
			_ => "gremlin_fat_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_fat", soundName);
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
