using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast;

public sealed class Pointy : CustomMonsterModel
{
	private const int AttackHits = 2;

	private static readonly LocString _deathReactLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-POINTY.deathReactLine");

	private const string STAB = "STAB";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 34, 30);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 34, 30);

	private int AttackDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 6, 5);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/pointy/pointy.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		Creature bear = ((IEnumerable<Creature>)((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.Monster is Bear));
		if (bear != null)
		{
			bear.Died += BearDeathResponse;
		}
	}

	private void BearDeathResponse(Creature _)
	{
		_.Died -= BearDeathResponse;
		if (!((MonsterModel)this).Creature.IsDead)
		{
			TalkCmd.Play(_deathReactLine, ((MonsterModel)this).Creature, (VfxColor)0, (VfxDuration)4);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		MoveState val = new MoveState("STAB", (Func<IReadOnlyList<Creature>, Task>)Stab, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(AttackDamage, 2) });
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { (MonsterState)(object)val }, (MonsterState)(object)val);
	}

	private async Task Stab(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Slash", 0f);
		await Cmd.Wait(0.4f, false);
		for (int i = 0; i < 2; i++)
		{
			await DamageCmd.Attack((decimal)AttackDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute((PlayerChoiceContext)null);
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
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Attack", false);
		AnimState val3 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("Slash", val2, (Func<bool>)null);
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
