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
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SlimeBoss : CustomMonsterModel
{
	private const string SLAM = "SLAM";

	private const string PREP_SLAM = "PREP_SLAM";

	private const string SPLIT = "SPLIT";

	private const string GOOP_SPRAY = "GOOP_SPRAY";

	private bool _splitTriggered;

	private MoveState _splitState;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 150, 140);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 150, 140);

	private int SlamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 38, 35);

	private int SlimedCount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/slime_boss/slime_boss.tscn";

	public override bool HasDeathSfx => false;

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)7;

	public bool SplitTriggered
	{
		get
		{
			return _splitTriggered;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_splitTriggered = value;
		}
	}

	public MoveState SplitState => _splitState;

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<SplitPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		((MonsterModel)this).Creature.Died += OnDeath;
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		AFTPModAudio.Play("slime_boss", "death");
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
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("GOOP_SPRAY", (Func<IReadOnlyList<Creature>, Task>)GoopSpray, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StatusIntent(SlimedCount) });
		MoveState val2 = new MoveState("PREP_SLAM", (Func<IReadOnlyList<Creature>, Task>)PrepSlam, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		MoveState val3 = new MoveState("SLAM", (Func<IReadOnlyList<Creature>, Task>)Slam, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SlamDamage) });
		_splitState = new MoveState("SPLIT", (Func<IReadOnlyList<Creature>, Task>)Split, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
		val.FollowUpState = (MonsterState)(object)val2;
		val2.FollowUpState = (MonsterState)(object)val3;
		val3.FollowUpState = (MonsterState)(object)conditionalBranchState;
		_splitState.FollowUpState = (MonsterState)(object)_splitState;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)_splitState);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (SplitTriggered)
		{
			return "SPLIT";
		}
		return "GOOP_SPRAY";
	}

	private async Task GoopSpray(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		AFTPModAudio.Play("general", "slime_attack");
		try
		{
			ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
			await CardPileCmd.AddToCombatAndPreview<Slimed>((IEnumerable<Creature>)targets, (PileType)3, SlimedCount, (Player)null, (CardPilePosition)1);
		}
		finally
		{
			ClassicSlimedTracker.CreatingClassicSlimed = false;
		}
	}

	private async Task PrepSlam(IReadOnlyList<Creature> targets)
	{
		PlayPrepSfx();
		TalkCmd.Play(MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-SLIME_BOSS.moves.PREP_SLAM.banter"), ((MonsterModel)this).Creature, (VfxColor)1, (VfxDuration)4);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)2, (ShakeDuration)3, -1f);
		}
		await Cmd.Wait(0.3f, false);
	}

	private async Task Slam(IReadOnlyList<Creature> targets)
	{
		await JumpAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			NCreature creatureNode = target.GetCreatureNode();
			if (creatureNode != null)
			{
				Node2D vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_heavy_blunt")).Instantiate<Node2D>((GenEditState)0);
				((CanvasItem)vfx).Modulate = Colors.Green;
				Control vfxContainer = target.GetVfxContainer();
				if (vfxContainer != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)vfxContainer, (Node)(object)vfx);
				}
				vfx.GlobalPosition = ((Control)creatureNode).GlobalPosition;
			}
		}
		await Cmd.Wait(0.4f, false);
		await DamageCmd.Attack((decimal)SlamDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Split(IReadOnlyList<Creature> targets)
	{
		int currentHp = ((MonsterModel)this).Creature.CurrentHp;
		ICombatState combatState = ((MonsterModel)this).Creature.CombatState;
		NCombatRoom instance = NCombatRoom.Instance;
		Vector2? obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NCreature creatureNode = instance.GetCreatureNode(((MonsterModel)this).Creature);
			obj = ((creatureNode != null) ? new Vector2?(((Control)creatureNode).Position) : ((Vector2?)null));
		}
		Vector2 originalPosition = (Vector2)(((_003F?)obj) ?? Vector2.Zero);
		ShakeAnimation.Play(((MonsterModel)this).Creature, 1f, 3f);
		await Cmd.Wait(1f, false);
		AFTPModAudio.Play("general", "slime_split");
		await CreatureCmd.Kill(((MonsterModel)this).Creature, false);
		HashSet<string> occupiedSlots = (from t in combatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t.IsAlive
			select t.SlotName).ToHashSet();
		string spikeSlot = combatState.Encounter.Slots?.FirstOrDefault((string s) => s.StartsWith("spike_large") && !occupiedSlots.Contains(s));
		string acidSlot = combatState.Encounter.Slots?.FirstOrDefault((string s) => s.StartsWith("acid_large") && !occupiedSlots.Contains(s));
		bool useSlots = spikeSlot != null && acidSlot != null;
		Queue<Vector2> positionQueue = null;
		NCombatRoom instance2 = NCombatRoom.Instance;
		Control enemyContainer = ((instance2 != null) ? ((Node)instance2).GetNode<Control>(NodePath.op_Implicit("%EnemyContainer")) : null);
		Callable? callable = null;
		if (!useSlots)
		{
			positionQueue = new Queue<Vector2>();
			callable = Callable.From<Node>((Action<Node>)OnChildEntered);
			if (enemyContainer != null)
			{
				((GodotObject)enemyContainer).Connect(SignalName.ChildEnteredTree, callable.Value, 0u);
			}
			positionQueue.Enqueue(originalPosition + new Vector2(-385f, 20f));
		}
		SpikeSlimeLarge spikeSlime = (SpikeSlimeLarge)(object)((MonsterModel)ModelDb.Monster<SpikeSlimeLarge>()).ToMutable();
		spikeSlime.OverrideHp = currentHp;
		Creature spikeCreature = await CreatureCmd.Add((MonsterModel)(object)spikeSlime, combatState, (CombatSide)2, spikeSlot);
		await CreatureCmd.SetMaxHp(spikeCreature, (decimal)currentHp);
		await CreatureCmd.Heal(spikeCreature, (decimal)currentHp, true);
		if (!useSlots)
		{
			positionQueue.Enqueue(originalPosition + new Vector2(120f, 20f));
		}
		AcidSlimeLarge acidSlime = (AcidSlimeLarge)(object)((MonsterModel)ModelDb.Monster<AcidSlimeLarge>()).ToMutable();
		acidSlime.OverrideHp = currentHp;
		Creature acidCreature = await CreatureCmd.Add((MonsterModel)(object)acidSlime, combatState, (CombatSide)2, acidSlot);
		await CreatureCmd.SetMaxHp(acidCreature, (decimal)currentHp);
		await CreatureCmd.Heal(acidCreature, (decimal)currentHp, true);
		if (!useSlots && callable.HasValue)
		{
			if (enemyContainer != null)
			{
				((GodotObject)enemyContainer).Disconnect(SignalName.ChildEnteredTree, callable.Value);
			}
		}
		void OnChildEntered(Node child)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			NCreature val = (NCreature)(object)((child is NCreature) ? child : null);
			if (val != null && positionQueue.Count > 0)
			{
				((Control)val).Position = positionQueue.Dequeue();
			}
		}
	}

	private void PlayPrepSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "slime_boss_talk_2" : "slime_boss_talk_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("slime_boss", soundName);
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
