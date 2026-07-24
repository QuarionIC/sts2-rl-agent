using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class AwakenedOne : CustomMonsterModel
{
	private const int SlashDamage = 20;

	private const int SoulStrikeDamage = 6;

	private const int SoulStrikeHits = 4;

	private const int DarkEchoDamage = 40;

	private const int SludgeDamage = 18;

	private const int TackleDamage = 10;

	private const int TackleHits = 3;

	private bool _particlesActive;

	private const float ParticleInterval = 0.1f;

	private static readonly LocString _deathDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-AWAKENED_ONE.deathLine");

	private const string SLASH = "SLASH";

	private const string SOUL_STRIKE = "SOUL_STRIKE";

	private const string REBIRTH = "REBIRTH";

	private const string DARK_ECHO = "DARK_ECHO";

	private const string SLUDGE = "SLUDGE";

	private const string TACKLE = "TACKLE";

	private MoveState _deadState;

	public int _respawns;

	private bool _saidPower;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 320, 300);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 320, 300);

	private int Phase2Hp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 320, 300);

	private int RegenAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 15, 10);

	private int CuriosityAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	private int StartingStrength => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 0);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/awakened_one/awakened_one.tscn";

	private MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_deadState = value;
		}
	}

	public int Respawns
	{
		get
		{
			return _respawns;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_respawns = value;
		}
	}

	public override bool ShouldDisappearFromDoom => Respawns >= 1;

	public async Task TriggerDeadState()
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "DeadTrigger", 0f);
		((MonsterModel)this).SetMoveImmediate(DeadState, true);
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		int curiosity = ((((MonsterModel)this).Creature.CombatState.Players.Count >= 2) ? 1 : CuriosityAmount);
		await PowerCmd.Apply<RegenEnemyPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)RegenAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<CuriosityPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)curiosity, ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<UnawakenedPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		if (StartingStrength > 0)
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StartingStrength, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		((MonsterModel)this).Creature.Died += OnParticleDeath;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SLASH", (Func<IReadOnlyList<Creature>, Task>)Slash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(20) });
		MoveState val2 = new MoveState("SOUL_STRIKE", (Func<IReadOnlyList<Creature>, Task>)SoulStrike, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(6, 4) });
		MoveState val3 = new MoveState("DARK_ECHO", (Func<IReadOnlyList<Creature>, Task>)DarkEcho, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(40) });
		MoveState val4 = new MoveState("SLUDGE", (Func<IReadOnlyList<Creature>, Task>)Sludge, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(18),
			(AbstractIntent)new StatusIntent(1)
		});
		MoveState val5 = new MoveState("TACKLE", (Func<IReadOnlyList<Creature>, Task>)Tackle, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(10, 3) });
		DeadState = new MoveState("REBIRTH", (Func<IReadOnlyList<Creature>, Task>)RebirthMove, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new HealIntent(),
			(AbstractIntent)new BuffIntent()
		})
		{
			MustPerformOnceBeforeTransitioning = true
		};
		ConditionalBranchState item = (ConditionalBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("PHASE1_BRANCH", SelectPhase1Move)));
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val5.FollowUpState = (val4.FollowUpState = (MonsterState)(object)new ConditionalBranchState("PHASE2_BRANCH", SelectPhase2Move)));
		DeadState.FollowUpState = (MonsterState)(object)val3;
		val3.FollowUpState = (MonsterState)(object)conditionalBranchState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)DeadState);
		list.Add((MonsterState)(object)item);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectPhase1Move(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 25)
		{
			if (!LastMove(stateMachine, "SOUL_STRIKE"))
			{
				return "SOUL_STRIKE";
			}
			return "SLASH";
		}
		if (!LastTwoMoves(stateMachine, "SLASH"))
		{
			return "SLASH";
		}
		return "SOUL_STRIKE";
	}

	private string SelectPhase2Move(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (num < 50)
		{
			if (!LastTwoMoves(stateMachine, "SLUDGE"))
			{
				return "SLUDGE";
			}
			return "TACKLE";
		}
		if (!LastTwoMoves(stateMachine, "TACKLE"))
		{
			return "TACKLE";
		}
		return "SLUDGE";
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

	private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 1].Id == moveId && stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task Slash(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("awakened_one", "awakened_one_pounce");
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Slash", 0f);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack(20m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task SoulStrike(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 4; i++)
		{
			await DamageCmd.Attack(6m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_fire_burst", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task DarkEcho(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("awakened_one", "awakened_one_talk_3");
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation("Attack_2", false, 0);
			MegaTrackEntry queued = animState.AddAnimationTracked("Idle_2", 0f, true, 0);
			try
			{
				if (queued != null)
				{
					queued.SetMixDuration(0.2f);
				}
			}
			finally
			{
				((IDisposable)queued)?.Dispose();
			}
		}
		await Cmd.Wait(0.1f, false);
		if (creatureNode != null)
		{
			Vector2 pos = creatureNode.VfxSpawnPosition;
			ShockWaveEffect.PlayChaotic(pos);
		}
		await DamageCmd.Attack(40m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Sludge(IReadOnlyList<Creature> targets)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation("Attack_2", false, 0);
			MegaTrackEntry queued = animState.AddAnimationTracked("Idle_2", 0f, true, 0);
			try
			{
				if (queued != null)
				{
					queued.SetMixDuration(0.2f);
				}
			}
			finally
			{
				((IDisposable)queued)?.Dispose();
			}
		}
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack(18m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_poison_impact", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] statusCards = (CardPileAddResult[])(object)new CardPileAddResult[1];
			Void voidCard = ((MonsterModel)this).CombatState.CreateCard<Void>(player);
			CardPileAddResult[] array = statusCards;
			array[0] = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)voidCard, (PileType)1, (Player)null, (CardPilePosition)3);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards, 1.2f, (CardPreviewStyle)1);
				await Cmd.Wait(1f, false);
			}
		}
	}

	private async Task Tackle(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("awakened_one", "awakened_one_attack");
		for (int i = 0; i < 3; i++)
		{
			await FastAttackAnimation.Play(((MonsterModel)this).Creature);
			await Cmd.Wait(0.06f, false);
			await DamageCmd.Attack(10m).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_fire_burst", (string)null, "blunt_attack.mp3")
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task RebirthMove(IReadOnlyList<Creature> targets)
	{
		Respawns++;
		AFTPModAudio.Play("awakened_one", "awakened_one_talk_1");
		StartParticleLoop();
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			Vector2 pos = creatureNode.VfxSpawnPosition;
			IntenseZoomEffect zoom = IntenseZoomEffect.Create(pos, isBlack: true);
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				Control combatVfxContainer = instance2.CombatVfxContainer;
				if (combatVfxContainer != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)zoom);
				}
			}
		}
		await Cmd.Wait(0.05f, false);
		MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
		if (spineBody != null)
		{
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation("Idle_2", true, 0);
			MegaTrackEntry trackEntry = animState.GetCurrent(0);
			if (trackEntry != null)
			{
				trackEntry.SetMixDuration(1f);
			}
		}
		await Cmd.Wait(1f, false);
		((MonsterModel)this).Creature.Powers.OfType<UnawakenedPower>().FirstOrDefault()?.DoRevive();
		int scaledHp = Phase2Hp * ((MonsterModel)this).Creature.CombatState.Players.Count;
		await CreatureCmd.SetMaxHp(((MonsterModel)this).Creature, (decimal)scaledHp);
		await CreatureCmd.Heal(((MonsterModel)this).Creature, (decimal)scaledHp, true);
		List<PowerModel> powersToRemove = ((MonsterModel)this).Creature.Powers.Where((PowerModel p) => (int)p.Type == 2 || p is CuriosityPower || p is UnawakenedPower).ToList();
		foreach (PowerModel power in powersToRemove)
		{
			await PowerCmd.Remove(power);
		}
	}

	private void StartParticleLoop()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		object obj;
		if (val == null)
		{
			obj = null;
		}
		else
		{
			NCreatureVisuals visuals = val.Visuals;
			obj = ((visuals != null) ? visuals.SpineBody : null);
		}
		MegaSprite val2 = (MegaSprite)obj;
		if (val2 == null || val == null)
		{
			return;
		}
		object obj2 = ((object)val2).GetType().GetMethod("GetSkeleton")?.Invoke(val2, null);
		if (obj2 == null)
		{
			return;
		}
		object obj3 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "Eye" });
		object obj4 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "Hips" });
		object? obj5 = obj3?.GetType().GetProperty("BoundObject")?.GetValue(obj3);
		GodotObject val3 = (GodotObject)((obj5 is GodotObject) ? obj5 : null);
		object? obj6 = obj4?.GetType().GetProperty("BoundObject")?.GetValue(obj4);
		GodotObject val4 = (GodotObject)((obj6 is GodotObject) ? obj6 : null);
		if (val3 != null || val4 != null)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val5 = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val5 != null)
			{
				_particlesActive = true;
				SpawnParticles(val, val3, val4, val5);
			}
		}
	}

	private void SpawnParticles(object creatureNode, GodotObject eyeBone, GodotObject hipsBone, SceneTree tree)
	{
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		if (!_particlesActive)
		{
			return;
		}
		try
		{
			dynamic val = ((dynamic)creatureNode).GlobalPosition;
			if (eyeBone != null)
			{
				float num = (float)eyeBone.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
				float num2 = (float)eyeBone.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
				Vector2 position = new Vector2(val.X + num * 1.1f, val.Y + num2 * 1.1f - 20f);
				AwakenedEyeParticle awakenedEyeParticle = AwakenedEyeParticle.Create(position);
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					Control combatVfxContainer = instance.CombatVfxContainer;
					if (combatVfxContainer != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)awakenedEyeParticle);
					}
				}
			}
			if (hipsBone != null)
			{
				NCombatRoom instance2 = NCombatRoom.Instance;
				NCreature creatureNode2 = ((instance2 != null) ? instance2.GetCreatureNode(((MonsterModel)this).Creature) : null);
				AwakenedWingParticle awakenedWingParticle = AwakenedWingParticle.Create(creatureNode2, hipsBone);
				NCombatRoom instance3 = NCombatRoom.Instance;
				if (instance3 != null)
				{
					Control combatVfxContainer2 = instance3.CombatVfxContainer;
					if (combatVfxContainer2 != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer2, (Node)(object)awakenedWingParticle);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		SceneTreeTimer val2 = tree.CreateTimer(0.10000000149011612, true, false, false);
		((GodotObject)val2).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
		{
			SpawnParticles(creatureNode, eyeBone, hipsBone, tree);
		}), 0u);
	}

	private void OnParticleDeath(Creature _)
	{
		_particlesActive = false;
		((MonsterModel)this).Creature.Died -= OnParticleDeath;
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		if (Respawns < 1)
		{
			string text = _deathDialog.GetFormattedText();
			NSpeechBubbleVfx bubble = NSpeechBubbleVfx.Create(text, ((MonsterModel)this).Creature, 2.5, (VfxColor)5);
			if (bubble != null)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)bubble);
				}
			}
			return;
		}
		List<Creature> cultists = ((MonsterModel)this).CombatState.Enemies.Where((Creature e) => e.IsAlive && e.Monster is Cultist).ToList();
		foreach (Creature cultist in cultists)
		{
			await CreatureCmd.Escape(cultist, true);
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
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		AnimState val = new AnimState("Idle_1", true);
		AnimState val2 = new AnimState("Attack_1", false);
		AnimState val3 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		AnimState val4 = new AnimState("Idle_2", true);
		val.AddBranch("Awaken", val4, (Func<bool>)null);
		CreatureAnimator val5 = new CreatureAnimator(val, controller);
		val5.AddAnyState("Slash", val2, (Func<bool>)(() => Respawns == 0));
		val5.AddAnyState("Hit", val3, (Func<bool>)(() => Respawns == 0));
		val5.AddAnyState("Phase2Hit", val4, (Func<bool>)(() => Respawns >= 1));
		return val5;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__1(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
