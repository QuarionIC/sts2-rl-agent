using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class SnakeDagger : CustomMonsterModel
{
	private const int StabDamage = 9;

	private const int SacrificeDamage = 25;

	private const string WOUND_STAB = "WOUND_STAB";

	private const string EXPLODE = "EXPLODE";

	public override int MinInitialHp => 20;

	public override int MaxInitialHp => 25;

	protected override string VisualsPath => "res://ActsFromThePast/monsters/dagger/dagger.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		MoveState val = new MoveState("WOUND_STAB", (Func<IReadOnlyList<Creature>, Task>)WoundStab, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(9),
			(AbstractIntent)new StatusIntent(1)
		});
		MoveState val2 = (MoveState)(object)(val.FollowUpState = (MonsterState)new MoveState("EXPLODE", (Func<IReadOnlyList<Creature>, Task>)Explode, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DeathBlowIntent((Func<decimal>)(() => 25m)) }));
		val2.FollowUpState = (MonsterState)(object)val2;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2
		}, (MonsterState)(object)val);
	}

	private async Task WoundStab(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Stab", 0f);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack(9m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		await CardPileCmd.AddToCombatAndPreview<Wound>((IEnumerable<Creature>)targets, (PileType)3, 1, (Player)null, (CardPilePosition)1);
	}

	private async Task Explode(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Suicide", 0f);
		await Cmd.Wait(0.4f, false);
		await DamageCmd.Attack(25m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		await CreatureCmd.Kill(((MonsterModel)this).Creature, false);
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
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Attack", false);
		AnimState val3 = new AnimState("Attack2", false);
		AnimState val4 = new AnimState("Hurt", false);
		val2.NextState = val;
		val4.NextState = val;
		CreatureAnimator val5 = new CreatureAnimator(val, controller);
		val5.AddAnyState("Stab", val2, (Func<bool>)null);
		val5.AddAnyState("Suicide", val3, (Func<bool>)null);
		val5.AddAnyState("Hurt", val4, (Func<bool>)null);
		return val5;
	}
}
