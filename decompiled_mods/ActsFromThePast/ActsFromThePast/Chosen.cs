using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Chosen : CustomMonsterModel
{
	private const int DebilitateVuln = 2;

	private const int DrainStrength = 3;

	private const int DrainWeak = 3;

	private const int HexAmount = 1;

	private const int PokeHits = 2;

	private static readonly LocString _hexDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHOSEN.moves.HEX.dialog");

	private const string ZAP = "ZAP";

	private const string DRAIN = "DRAIN";

	private const string DEBILITATE = "DEBILITATE";

	private const string HEX = "HEX";

	private const string POKE = "POKE";

	private bool _usedHex;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 98, 95);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 103, 99);

	private int ZapDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 21, 18);

	private int DebilitateDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	private int PokeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 6, 5);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/chosen/chosen.tscn";

	private bool UsedHex
	{
		get
		{
			return _usedHex;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedHex = value;
		}
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__0(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			AFTPModAudio.Play("chosen", "chosen_death");
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
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("HEX", (Func<IReadOnlyList<Creature>, Task>)HexMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val2 = new MoveState("DEBILITATE", (Func<IReadOnlyList<Creature>, Task>)DebilitateMove, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(DebilitateDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val3 = new MoveState("DRAIN", (Func<IReadOnlyList<Creature>, Task>)DrainMove, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DebuffIntent(false),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val4 = new MoveState("ZAP", (Func<IReadOnlyList<Creature>, Task>)ZapMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(ZapDamage) });
		MoveState val5 = new MoveState("POKE", (Func<IReadOnlyList<Creature>, Task>)PokeMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(PokeDamage, 2) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val5.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (!UsedHex)
		{
			UsedHex = true;
			return "HEX";
		}
		if (!LastMove(stateMachine, "DEBILITATE") && !LastMove(stateMachine, "DRAIN"))
		{
			return (rng.NextInt(100) < 50) ? "DEBILITATE" : "DRAIN";
		}
		return (rng.NextInt(100) < 40) ? "ZAP" : "POKE";
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

	private async Task HexMove(IReadOnlyList<Creature> targets)
	{
		TalkCmd.Play(_hexDialog, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Hex", 0f);
		await Cmd.Wait(0.2f, false);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<HexOriginalPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task ZapMove(IReadOnlyList<Creature> targets)
	{
		await ShakeAnimation.Play(((MonsterModel)this).Creature, 0.5f, 0.3f);
		await DamageCmd.Attack((decimal)ZapDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitFx("vfx/vfx_fire_burst", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task DrainMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task DebilitateMove(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)DebilitateDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task PokeMove(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		for (int i = 0; i < 2; i++)
		{
			await DamageCmd.Attack((decimal)PokeDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
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
		val4.AddAnyState("Hex", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		controller.GetAnimationState().SetTimeScale(0.8f);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
