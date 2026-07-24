using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Act4Heart.Powers;
using Dolso;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace Act4Heart;

internal class CorruptHeart : MonsterModel
{
	private byte state;

	private int buff_counter;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 800, 750);

	public override int MaxInitialHp => ((MonsterModel)this).MinInitialHp;

	private static int blood_shots_damage => 2;

	private static int blood_shots_count => 15;

	private static int echo_damage => 45;

	private static int beat_initial_amount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	private static decimal invincible_initial_amount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 200m, 300m);

	public override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		MoveState val = new MoveState("DEBILITATE_MOVE", (Func<IReadOnlyList<Creature>, Task>)debilite_move, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DebuffIntent(true),
			(AbstractIntent)new StatusIntent(5)
		});
		MoveState val2 = new MoveState("BLOOD_SHOTS_MOVE", (Func<IReadOnlyList<Creature>, Task>)blod_shots_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(blood_shots_damage, blood_shots_count) });
		MoveState val3 = new MoveState("ECHO_MOVE", (Func<IReadOnlyList<Creature>, Task>)echo_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(echo_damage) });
		MoveState val4 = new MoveState("BUFF_MOVE", (Func<IReadOnlyList<Creature>, Task>)buff_move, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		RandomBranchState val5 = new RandomBranchState("RANDOM_ATTACK_BRANCH");
		ConditionalBranchState val6 = new ConditionalBranchState("POST_ATTACK_BRANCH");
		val.FollowUpState = (MonsterState)(object)val5;
		val5.AddBranch((MonsterState)(object)val2, (MoveRepeatType)0);
		val5.AddBranch((MonsterState)(object)val3, (MoveRepeatType)0);
		state = 0;
		val2.FollowUpState = (MonsterState)(object)val6;
		val3.FollowUpState = (MonsterState)(object)val6;
		val6.AddState((MonsterState)(object)val2, (Func<bool>)(() => (state & 1) == 0));
		val6.AddState((MonsterState)(object)val3, (Func<bool>)(() => (state & 2) == 0));
		val6.AddState((MonsterState)(object)val4, (Func<bool>)(() => (state & 3) == 3));
		val4.FollowUpState = (MonsterState)(object)val5;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new _003C_003Ez__ReadOnlyArray<MonsterState>((MonsterState[])(object)new MonsterState[6]
		{
			(MonsterState)val,
			(MonsterState)val2,
			(MonsterState)val3,
			(MonsterState)val4,
			(MonsterState)val5,
			(MonsterState)val6
		}), (MonsterState)(object)val);
	}

	private async Task debilite_move(IReadOnlyList<Creature> targets)
	{
		state = 0;
		await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)targets, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)targets, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)targets, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		CardPileAddResult[] statuses = (CardPileAddResult[])(object)new CardPileAddResult[5];
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] array = statuses;
			array[0] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((MonsterModel)this).CombatState.CreateCard<Dazed>(player), (PileType)1, (Player)null, (CardPilePosition)3);
			array = statuses;
			array[1] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((MonsterModel)this).CombatState.CreateCard<Slimed>(player), (PileType)1, (Player)null, (CardPilePosition)3);
			array = statuses;
			array[2] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((MonsterModel)this).CombatState.CreateCard<Wound>(player), (PileType)1, (Player)null, (CardPilePosition)3);
			array = statuses;
			array[3] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((MonsterModel)this).CombatState.CreateCard<Burn>(player), (PileType)1, (Player)null, (CardPilePosition)3);
			array = statuses;
			array[4] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((MonsterModel)this).CombatState.CreateCard<Void>(player), (PileType)1, (Player)null, (CardPilePosition)3);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statuses, 1f, (CardPreviewStyle)1);
			}
		}
		await Cmd.Wait(1f, false);
	}

	private async Task blod_shots_move(IReadOnlyList<Creature> targets)
	{
		state |= 1;
		await DamageCmd.Attack((decimal)blood_shots_damage).WithHitCount(blood_shots_count).WithHitFx("vfx/vfx_bloody_impact", (string)null, "slash_attack.mp3")
			.FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task echo_move(IReadOnlyList<Creature> targets)
	{
		state |= 2;
		await DamageCmd.Attack((decimal)echo_damage).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3").FromMonster((MonsterModel)(object)this)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task buff_move(IReadOnlyList<Creature> targets)
	{
		state = 0;
		if (((MonsterModel)this).Creature.GetPowerAmount<StrengthPower>() < 0)
		{
			await PowerCmd.Remove<StrengthPower>(((MonsterModel)this).Creature);
		}
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		buff_counter++;
		await (buff_counter switch
		{
			1 => PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 2m, ((MonsterModel)this).Creature, (CardModel)null, false), 
			2 => PowerCmd.Apply<BeatOfDeathPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false), 
			3 => PowerCmd.Apply<PainfulStabsPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false), 
			4 => PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 10m, ((MonsterModel)this).Creature, (CardModel)null, false), 
			_ => PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 50m, ((MonsterModel)this).Creature, (CardModel)null, false), 
		});
	}

	public override async Task AfterAddedToRoom()
	{
		log.info("Applying Heart powers");
		await PowerCmd.Apply<BeatOfDeathPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)beat_initial_amount, ((MonsterModel)this).Creature, (CardModel)null, false);
		decimal num = ((((IPlayerCollection)((MonsterModel)this).CombatState.RunState).Players.Count <= 1) ? invincible_initial_amount : (ModMain.current_config.multiplayer_heart_split_invincible_pool ? (invincible_initial_amount * (decimal)ModMain.current_config.multiplayer_heart_health_scaling_coef) : (invincible_initial_amount * (decimal)((IPlayerCollection)((MonsterModel)this).CombatState.RunState).Players.Count)));
		await PowerCmd.Apply<InvinciblePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, num, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature == ((MonsterModel)this).Creature && !wasRemovalPrevented)
		{
			NRunMusicController instance = NRunMusicController.Instance;
			if (instance != null)
			{
				instance.UpdateMusic();
			}
		}
		return Task.CompletedTask;
	}
}
