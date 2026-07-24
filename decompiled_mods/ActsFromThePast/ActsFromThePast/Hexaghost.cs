using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Patches.Audio;
using ActsFromThePast.Patches.Cards;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Hexaghost : CustomMonsterModel
{
	private const int SearDamage = 6;

	private const int FireTackleCount = 2;

	private const int InfernoHits = 6;

	private const int StrengthenBlock = 12;

	private bool _activated;

	private bool _burnUpgraded;

	private int _orbActiveCount;

	private int _dividerDamage;

	private const string ACTIVATE = "ACTIVATE";

	private const string DIVIDER = "DIVIDER";

	private const string TACKLE = "TACKLE";

	private const string INFLAME = "INFLAME";

	private const string SEAR = "SEAR";

	private const string INFERNO = "INFERNO";

	private MoveState _dividerState;

	private HexaghostVisuals? _visuals;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 264, 250);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 264, 250);

	private int InfernoDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 3, 2);

	private int FireTackleDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 6, 5);

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 3, 2);

	private int SearBurnCount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/hexaghost/hexaghost.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_activated = false;
		_burnUpgraded = false;
		_orbActiveCount = 0;
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			_visuals = new HexaghostVisuals(((MonsterModel)this).Creature, creatureNode);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Expected O, but got Unknown
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("ACTIVATE", (Func<IReadOnlyList<Creature>, Task>)Activate, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		_dividerState = new MoveState("DIVIDER", (Func<IReadOnlyList<Creature>, Task>)Divider, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicMultiAttackIntent(() => _dividerDamage, 6) });
		MoveState val2 = new MoveState("TACKLE", (Func<IReadOnlyList<Creature>, Task>)Tackle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(FireTackleDamage, 2) });
		MoveState val3 = new MoveState("INFLAME", (Func<IReadOnlyList<Creature>, Task>)Inflame, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val4 = new MoveState("SEAR", (Func<IReadOnlyList<Creature>, Task>)Sear, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(6),
			(AbstractIntent)new StatusIntent(SearBurnCount)
		});
		MoveState val5 = new MoveState("INFERNO", (Func<IReadOnlyList<Creature>, Task>)Inferno, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new MultiAttackIntent(InfernoDamage, 6),
			(AbstractIntent)new DebuffIntent(false)
		});
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
		val.FollowUpState = (MonsterState)(object)_dividerState;
		_dividerState.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val2.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val3.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val4.FollowUpState = (MonsterState)(object)conditionalBranchState;
		val5.FollowUpState = (MonsterState)(object)conditionalBranchState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)_dividerState);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		return _orbActiveCount switch
		{
			0 => "SEAR", 
			1 => "TACKLE", 
			2 => "SEAR", 
			3 => "INFLAME", 
			4 => "TACKLE", 
			5 => "SEAR", 
			6 => "INFERNO", 
			_ => "SEAR", 
		};
	}

	private async Task Activate(IReadOnlyList<Creature> targets)
	{
		_activated = true;
		_orbActiveCount = 6;
		_visuals?.ActivateAllOrbs();
		_visuals?.SetTargetRotationSpeed(120f);
		if (!MindBloom.CombatActive)
		{
			MusicPatches.LegacyActMusicPatches.OnHexaghostActivated();
		}
		List<Creature> livingTargets = targets.Where((Creature t) => t.IsAlive).ToList();
		double averageHp = ((livingTargets.Count > 0) ? livingTargets.Average((Creature t) => t.CurrentHp) : 1.0);
		_dividerDamage = (int)(averageHp / 12.0) + 1;
	}

	private async Task Divider(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 6; i++)
		{
			foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
			{
				Vector2 playerCenter = Sts1VfxHelper.GetCreatureCenter(target);
				float offsetX = (float)(Rng.Chaotic.NextDouble() * 240.0 - 120.0);
				float offsetY = (float)(Rng.Chaotic.NextDouble() * 240.0 - 120.0);
				GhostIgniteEffect.Create(playerCenter.X + offsetX, playerCenter.Y + offsetY);
			}
			await Cmd.Wait(0.05f, false);
			await DamageCmd.Attack((decimal)_dividerDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
				.WithHitVfxNode((Func<Creature, Node2D>)((Creature target2) => CreateGhostFireBurst(target2)))
				.Execute((PlayerChoiceContext)null);
		}
		await DeactivateAllOrbs();
	}

	public static Node2D? CreateGhostFireBurst(Creature target)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val == null || !val.IsInteractable)
		{
			return null;
		}
		Node2D val2 = PreloadManager.Cache.GetScene("scenes/vfx/vfx_fire_burst.tscn").Instantiate<Node2D>((GenEditState)0);
		val2.GlobalPosition = val.VfxSpawnPosition;
		((CanvasItem)val2).Modulate = new Color(0.455f, 0.918f, 0.027f, 1f);
		return val2;
	}

	private async Task Tackle(IReadOnlyList<Creature> targets)
	{
		BorderFlashEffect.PlayChartreuse();
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)FireTackleDamage).WithHitCount(2).FromMonster((MonsterModel)(object)this)
			.WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitVfxNode((Func<Creature, Node2D>)((Creature target) => CreateGhostFireBurst(target)))
			.Execute((PlayerChoiceContext)null);
		await ActivateOrb();
	}

	private async Task Inflame(IReadOnlyList<Creature> targets)
	{
		NPowerUpVfx.CreateGhostly(((MonsterModel)this).Creature);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 12m, (ValueProp)8, (CardPlay)null, false);
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		await ActivateOrb();
	}

	private async Task Sear(IReadOnlyList<Creature> targets)
	{
		Creature playerCreature = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature c) => c.Player != null));
		if (playerCreature != null)
		{
			Vector2 startPos = Sts1VfxHelper.GetCreatureCenter(((MonsterModel)this).Creature);
			Vector2 targetPos = Sts1VfxHelper.GetCreatureCenter(playerCreature);
			FireballEffect fireball = FireballEffect.Create(startPos, targetPos);
			Sts1VfxHelper.Play(fireball);
			await Cmd.Wait(0.5f, false);
		}
		await DamageCmd.Attack(6m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitVfxNode((Func<Creature, Node2D>)((Creature target) => CreateGhostFireBurst(target)))
			.Execute((PlayerChoiceContext)null);
		await AddBurnsToDiscard(targets, SearBurnCount);
		await ActivateOrb();
	}

	private async Task Inferno(IReadOnlyList<Creature> targets)
	{
		ScreenOnFireEffect screenFire = ScreenOnFireEffect.Create();
		Sts1VfxHelper.Play(screenFire);
		await Cmd.Wait(1f, false);
		await DamageCmd.Attack((decimal)InfernoDamage).WithHitCount(6).FromMonster((MonsterModel)(object)this)
			.WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitVfxNode((Func<Creature, Node2D>)((Creature target) => CreateGhostFireBurst(target)))
			.Execute((PlayerChoiceContext)null);
		await UpgradeAllBurnsAndAddMore(targets);
		_burnUpgraded = true;
		await DeactivateAllOrbs();
	}

	private Task ActivateOrb()
	{
		_orbActiveCount++;
		_visuals?.ActivateNextOrb();
		return Task.CompletedTask;
	}

	private Task DeactivateAllOrbs()
	{
		_orbActiveCount = 0;
		_visuals?.DeactivateAllOrbs();
		PlayExhaustSfx();
		PlayExhaustSfx();
		return Task.CompletedTask;
	}

	private async Task UpgradeAllBurnsAndAddMore(IReadOnlyList<Creature> targets)
	{
		BurnUpgradePatch.AllowBurnUpgrade = true;
		try
		{
			foreach (Creature playerCreature in targets.Where((Creature t) => t.Player != null))
			{
				Player player = playerCreature.Player;
				List<Burn> burnsToUpgrade = (from b in player.Piles.Where((CardPile p) => (int)p.Type == 1 || (int)p.Type == 3 || (int)p.Type == 2).SelectMany((CardPile p) => p.Cards).OfType<Burn>()
					where ((CardModel)b).IsUpgradable
					select b).ToList();
				foreach (Burn burn in burnsToUpgrade)
				{
					((CardModel)burn).UpgradeInternal();
					((CardModel)burn).FinalizeUpgradeInternal();
				}
				ICombatState combatState = playerCreature.CombatState;
				CardPileAddResult[] statusCards = (CardPileAddResult[])(object)new CardPileAddResult[3];
				for (int i = 0; i < 3; i++)
				{
					Burn burn2 = combatState.CreateCard<Burn>(player);
					((CardModel)burn2).UpgradeInternal();
					((CardModel)burn2).FinalizeUpgradeInternal();
					CardPileAddResult[] array = statusCards;
					int num = i;
					array[num] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)burn2, (PileType)3, (Player)null, (CardPilePosition)1);
				}
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards, 1.2f, (CardPreviewStyle)1);
			}
			await Cmd.Wait(1f, false);
		}
		finally
		{
			BurnUpgradePatch.AllowBurnUpgrade = false;
		}
	}

	private async Task AddBurnsToDiscard(IReadOnlyList<Creature> targets, int count)
	{
		if (!_burnUpgraded)
		{
			await CardPileCmd.AddToCombatAndPreview<Burn>((IEnumerable<Creature>)targets, (PileType)3, count, (Player)null, (CardPilePosition)1);
			return;
		}
		BurnUpgradePatch.AllowBurnUpgrade = true;
		try
		{
			foreach (Creature playerCreature in targets.Where((Creature t) => t.Player != null))
			{
				Player player = playerCreature.Player;
				ICombatState combatState = playerCreature.CombatState;
				CardPileAddResult[] statusCards = (CardPileAddResult[])(object)new CardPileAddResult[count];
				for (int i = 0; i < count; i++)
				{
					Burn burn = combatState.CreateCard<Burn>(player);
					((CardModel)burn).UpgradeInternal();
					((CardModel)burn).FinalizeUpgradeInternal();
					CardPileAddResult[] array = statusCards;
					int num = i;
					array[num] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)burn, (PileType)3, (Player)null, (CardPilePosition)1);
				}
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards, 1.2f, (CardPreviewStyle)((count <= 5) ? 1 : 2));
			}
			await Cmd.Wait(1f, false);
		}
		finally
		{
			BurnUpgradePatch.AllowBurnUpgrade = false;
		}
	}

	private void PlayGhostOrbIgniteSfx()
	{
		string soundName = ((Rng.Chaotic.NextInt(2) == 0) ? "ghost_orb_ignite_1" : "ghost_orb_ignite_2");
		AFTPModAudio.Play("hexaghost", soundName);
	}

	private void PlayExhaustSfx()
	{
		NDebugAudioManager instance = NDebugAudioManager.Instance;
		if (instance != null)
		{
			instance.Play("card_exhaust.mp3", 1f, (PitchVariance)0);
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		await _003C_003En__1(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
		if (creature == ((MonsterModel)this).Creature)
		{
			_visuals?.HideAllOrbs();
			_visuals?.Dispose();
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)4, (ShakeDuration)3, -1f);
			}
		}
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__1(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		return ((AbstractModel)this).AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
	}
}
