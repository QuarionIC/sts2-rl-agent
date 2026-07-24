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
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class GremlinShield : CustomMonsterModel
{
	private const string PROTECT = "PROTECT";

	private const string SHIELD_BASH = "SHIELD_BASH";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 13, 12);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 17, 15);

	private int ProtectBlock => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 11, 7);

	private int ShieldBashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 8, 6);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_shield/gremlin_shield.tscn";

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
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "gremlin_shield_death_2" : "gremlin_shield_death_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_shield", soundName);
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
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("PROTECT", (Func<IReadOnlyList<Creature>, Task>)Protect, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val2 = new MoveState("SHIELD_BASH", (Func<IReadOnlyList<Creature>, Task>)ShieldBash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(ShieldBashDamage) });
		ConditionalBranchState item = (ConditionalBranchState)(object)(val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove));
		val2.FollowUpState = (MonsterState)(object)val2;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int count = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Count;
		return (count > 1) ? "PROTECT" : "SHIELD_BASH";
	}

	private async Task Protect(IReadOnlyList<Creature> targets)
	{
		IEnumerable<Creature> teammates = from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t;
		Creature target = (teammates.Any() ? ((MonsterModel)this).Rng.NextItem<Creature>(teammates) : ((MonsterModel)this).Creature);
		await CreatureCmd.GainBlock(target, (decimal)ProtectBlock, (ValueProp)8, (CardPlay)null, false);
	}

	private async Task ShieldBash(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)ShieldBashDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
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
