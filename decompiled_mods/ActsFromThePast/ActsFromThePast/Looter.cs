using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Looter : CustomMonsterModel
{
	private const int EscapeBlock = 6;

	private const string MUG = "MUG";

	private const string SMOKE_BOMB = "SMOKE_BOMB";

	private const string ESCAPE = "ESCAPE";

	private const string LUNGE = "LUNGE";

	private int _mugCount;

	private bool _hasSpoken;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 46, 44);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 50, 48);

	private int MugDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 11, 10);

	private int LungeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 14, 12);

	private int GoldAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 20, 15);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/looter/looter.tscn";

	public override bool HasDeathSfx => false;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		foreach (Player player in ((MonsterModel)this).Creature.CombatState.Players)
		{
			ThieveryPower thievery = (ThieveryPower)((PowerModel)ModelDb.Power<ThieveryPower>()).ToMutable(0);
			((PowerModel)thievery).Target = player.Creature;
			await PowerCmd.Apply((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (PowerModel)(object)thievery, ((MonsterModel)this).Creature, (decimal)GoldAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		((MonsterModel)this).Creature.Died -= OnDeath;
		AbstractRoom currentRoom = ((MonsterModel)this).Creature.CombatState.RunState.CurrentRoom;
		CombatRoom val = (CombatRoom)(object)((currentRoom is CombatRoom) ? currentRoom : null);
		if (val != null)
		{
			foreach (ThieveryPower powerInstance in ((MonsterModel)this).Creature.GetPowerInstances<ThieveryPower>())
			{
				int intValue = ((DynamicVar)((PowerModel)powerInstance).DynamicVars.Gold).IntValue;
				if (intValue > 0)
				{
					val.AddExtraReward(((PowerModel)powerInstance).Target.Player, (Reward)new GoldReward(intValue, ((PowerModel)powerInstance).Target.Player, true));
				}
			}
		}
		if (Rng.Chaotic.NextFloat(1f) < 0.3f)
		{
			TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.deathLine"), ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		}
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
			0 => "looter_death_1", 
			1 => "looter_death_2", 
			_ => "looter_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("looter", soundName);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("MUG", (Func<IReadOnlyList<Creature>, Task>)Mug, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(MugDamage) });
		MoveState val2 = new MoveState("SMOKE_BOMB", (Func<IReadOnlyList<Creature>, Task>)SmokeBomb, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val3 = new MoveState("ESCAPE", (Func<IReadOnlyList<Creature>, Task>)Escape, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new EscapeIntent() });
		MoveState val4 = new MoveState("LUNGE", (Func<IReadOnlyList<Creature>, Task>)Lunge, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(LungeDamage) });
		RandomBranchState val5 = new RandomBranchState("AFTER_SECOND_MUG");
		val5.AddBranch((MonsterState)(object)val2, (MoveRepeatType)3, 50f);
		val5.AddBranch((MonsterState)(object)val4, (MoveRepeatType)3, 50f);
		ConditionalBranchState item = (ConditionalBranchState)(object)(val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MUG_BRANCH", SelectAfterMug));
		val2.FollowUpState = (MonsterState)(object)val3;
		val3.FollowUpState = (MonsterState)(object)val3;
		val4.FollowUpState = (MonsterState)(object)val2;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectAfterMug(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (_mugCount < 2)
		{
			return "MUG";
		}
		return "AFTER_SECOND_MUG";
	}

	private async Task Mug(IReadOnlyList<Creature> targets)
	{
		if (!_hasSpoken && Rng.Chaotic.NextFloat(1f) < 0.6f)
		{
			_hasSpoken = true;
			TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.MUG.banter"), ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		}
		PlayAttackSfx();
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		VfxCmd.PlayOnCreatureCenters((IEnumerable<Creature>)targets, "vfx/vfx_coin_explosion_regular");
		await DamageCmd.Attack((decimal)MugDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		await StealGold();
		_mugCount++;
	}

	private async Task SmokeBomb(IReadOnlyList<Creature> targets)
	{
		TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.SMOKE_BOMB.banter"), ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 6m, (ValueProp)8, (CardPlay)null, false);
	}

	private async Task Escape(IReadOnlyList<Creature> targets)
	{
		TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.ESCAPE.banter"), ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)3);
		MapPointHistoryEntry entry = ((MonsterModel)this).Creature.CombatState.RunState.CurrentMapPointHistoryEntry;
		foreach (ThieveryPower thievery in ((MonsterModel)this).Creature.GetPowerInstances<ThieveryPower>())
		{
			int stolen = ((DynamicVar)((PowerModel)thievery).DynamicVars.Gold).IntValue;
			if (stolen <= 0)
			{
				continue;
			}
			if (entry != null)
			{
				PlayerMapPointHistoryEntry entry2 = entry.GetEntry(((PowerModel)thievery).Target.Player.NetId);
				if (entry2 != null)
				{
					entry2.MarkLootStolen(stolen);
				}
			}
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			SmokeBombEffect effect = SmokeBombEffect.Create(creatureNode.VfxSpawnPosition);
			((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
		}
		if (creatureNode != null)
		{
			creatureNode.ToggleIsInteractable(false);
		}
		await EscapeAnimation.Play(((MonsterModel)this).Creature);
		await CreatureCmd.Escape(((MonsterModel)this).Creature, true);
	}

	private async Task Lunge(IReadOnlyList<Creature> targets)
	{
		PlayAttackSfx();
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		VfxCmd.PlayOnCreatureCenters((IEnumerable<Creature>)targets, "vfx/vfx_coin_explosion_regular");
		await DamageCmd.Attack((decimal)LungeDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
		await StealGold();
		_mugCount++;
	}

	private async Task StealGold()
	{
		foreach (ThieveryPower thievery in ((MonsterModel)this).Creature.GetPowerInstances<ThieveryPower>())
		{
			Creature target = ((PowerModel)thievery).Target;
			if (target != null && !target.IsDead && target.Player.Gold > 0)
			{
				int amount = Math.Min(((PowerModel)thievery).Amount, target.Player.Gold);
				await PlayerCmd.LoseGold((decimal)amount, target.Player, (GoldLossType)2);
				GoldVar gold = ((PowerModel)thievery).DynamicVars.Gold;
				((DynamicVar)gold).BaseValue = ((DynamicVar)gold).BaseValue + (decimal)amount;
			}
		}
	}

	private void PlayAttackSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "looter_talk_1", 
			1 => "looter_talk_2", 
			_ => "looter_talk_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("looter", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		return new CreatureAnimator(val, controller);
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
