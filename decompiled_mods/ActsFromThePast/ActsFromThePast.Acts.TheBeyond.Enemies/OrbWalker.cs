using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class OrbWalker : CustomMonsterModel
{
	private const string LASER = "LASER";

	private const string CLAW = "CLAW";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 92, 90);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 102, 96);

	private int LaserDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 11, 10);

	private int ClawDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 16, 15);

	private int StrengthUpAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/orb_walker/orb_walker.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<StrengthUpPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrengthUpAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("LASER", (Func<IReadOnlyList<Creature>, Task>)Laser, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(LaserDamage),
			(AbstractIntent)new StatusIntent(2)
		});
		MoveState val2 = new MoveState("CLAW", (Func<IReadOnlyList<Creature>, Task>)Claw, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(ClawDamage) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove)));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 40)
		{
			if (!LastTwoMoves(stateMachine, "CLAW"))
			{
				return "CLAW";
			}
			return "LASER";
		}
		if (!LastTwoMoves(stateMachine, "LASER"))
		{
			return "LASER";
		}
		return "CLAW";
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

	private async Task Laser(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Laser", 0f);
		await Cmd.Wait(0.4f, false);
		await DamageCmd.Attack((decimal)LaserDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitFx("vfx/vfx_fire_burst", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] statusCards = (CardPileAddResult[])(object)new CardPileAddResult[2];
			Burn burn1 = ((MonsterModel)this).CombatState.CreateCard<Burn>(player);
			CardPileAddResult[] array = statusCards;
			array[0] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)burn1, (PileType)3, (Player)null, (CardPilePosition)1);
			Burn burn2 = ((MonsterModel)this).CombatState.CreateCard<Burn>(player);
			CardPileAddResult[] array2 = statusCards;
			array2[1] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)burn2, (PileType)1, (Player)null, (CardPilePosition)3);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards, 1.2f, (CardPreviewStyle)1);
				await Cmd.Wait(1f, false);
			}
		}
	}

	private async Task Claw(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)ClawDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
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
		val4.AddAnyState("Laser", val2, (Func<bool>)null);
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
