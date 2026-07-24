using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Collector : CustomMonsterModel
{
	private static readonly LocString _megaDebuffDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-COLLECTOR.moves.MEGA_DEBUFF.dialog");

	private const string SPAWN = "SPAWN";

	private const string FIREBALL = "FIREBALL";

	private const string BUFF = "BUFF";

	private const string MEGA_DEBUFF = "MEGA_DEBUFF";

	private const string REVIVE = "REVIVE";

	private const float FireInterval = 0.07f;

	private int _turnsTaken;

	private bool _ultUsed;

	private bool _initialSpawn;

	private bool _alive = true;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 300, 282);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 300, 282);

	private int FireballDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 21, 18);

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 4);

	private int BlockAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 18, 15);

	private int MegaDebuffAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/collector/collector.tscn";

	private int TurnsTaken
	{
		get
		{
			return _turnsTaken;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_turnsTaken = value;
		}
	}

	private bool UltUsed
	{
		get
		{
			return _ultUsed;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_ultUsed = value;
		}
	}

	private bool InitialSpawn
	{
		get
		{
			return _initialSpawn;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_initialSpawn = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_turnsTaken = 0;
		_ultUsed = false;
		_initialSpawn = true;
		_alive = true;
		((MonsterModel)this).Creature.Died += OnCollectorDeath;
		StartFireLoop();
	}

	private void OnCollectorDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnCollectorDeath;
		_alive = false;
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)4, (ShakeDuration)3, -1f);
		}
		List<Creature> livingMinions = (from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t != ((MonsterModel)this).Creature && t.IsAlive
			select t).ToList();
		foreach (Creature minion in livingMinions)
		{
			await CreatureCmd.Kill(minion, false);
		}
	}

	private void StartFireLoop()
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
		if (obj2 != null)
		{
			GodotObject leftEye = FindBone(obj2, "lefteyefireslot");
			GodotObject rightEye = FindBone(obj2, "righteyefireslot");
			GodotObject staff = FindBone(obj2, "fireslot");
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val3 = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val3 != null)
			{
				SpawnFireParticles(val, leftEye, rightEye, staff, val3);
			}
		}
	}

	private GodotObject FindBone(object skeleton, string boneName)
	{
		object obj = skeleton.GetType().GetMethod("FindBone")?.Invoke(skeleton, new object[1] { boneName });
		object? obj2 = obj?.GetType().GetProperty("BoundObject")?.GetValue(obj);
		return (GodotObject)((obj2 is GodotObject) ? obj2 : null);
	}

	private void SpawnFireParticles(object creatureNode, GodotObject leftEye, GodotObject rightEye, GodotObject staff, SceneTree tree)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		if (!_alive)
		{
			return;
		}
		try
		{
			Vector2 creatureGlobalPos = GetCreatureGlobalPos(creatureNode);
			NCombatRoom instance = NCombatRoom.Instance;
			Node val = (Node)(object)((instance != null) ? instance.CombatVfxContainer : null);
			if (val != null)
			{
				if (leftEye != null)
				{
					Vector2 boneWorldPosition = GetBoneWorldPosition(leftEye, creatureGlobalPos);
					GlowyFireEyesEffect glowyFireEyesEffect = GlowyFireEyesEffect.Create(boneWorldPosition);
					GodotTreeExtensions.AddChildSafely(val, (Node)(object)glowyFireEyesEffect);
				}
				if (rightEye != null)
				{
					Vector2 boneWorldPosition2 = GetBoneWorldPosition(rightEye, creatureGlobalPos);
					GlowyFireEyesEffect glowyFireEyesEffect2 = GlowyFireEyesEffect.Create(boneWorldPosition2);
					GodotTreeExtensions.AddChildSafely(val, (Node)(object)glowyFireEyesEffect2);
				}
				if (staff != null)
				{
					Vector2 boneWorldPosition3 = GetBoneWorldPosition(staff, creatureGlobalPos);
					boneWorldPosition3.Y -= 15f;
					StaffFireEffect staffFireEffect = StaffFireEffect.Create(boneWorldPosition3);
					((CanvasItem)staffFireEffect).ZIndex = -1;
					GodotTreeExtensions.AddChildSafely(val, (Node)(object)staffFireEffect);
				}
				SceneTreeTimer val2 = tree.CreateTimer(0.07000000029802322, true, false, false);
				((GodotObject)val2).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
				{
					SpawnFireParticles(creatureNode, leftEye, rightEye, staff, tree);
				}), 0u);
			}
		}
		catch (Exception ex)
		{
			Log.Info("[Collector] Fire particle error: " + ex.Message, 2);
		}
	}

	private static Vector2 GetCreatureGlobalPos(object creatureNode)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		return (Vector2)((dynamic)creatureNode).GlobalPosition;
	}

	private Vector2 GetBoneWorldPosition(GodotObject bone, Vector2 creatureGlobalPos)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)bone.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
		float num2 = (float)bone.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
		return new Vector2(creatureGlobalPos.X + num * 1.1f, creatureGlobalPos.Y + num2 * 1.1f);
	}

	private bool IsMinionDead()
	{
		int num = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature).Count((Creature t) => t != ((MonsterModel)this).Creature && t.IsAlive);
		int num2 = ((MonsterModel)this).CombatState.Encounter.Slots.Count((string s) => s.StartsWith("torch"));
		return num < num2;
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
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("SPAWN", (Func<IReadOnlyList<Creature>, Task>)SpawnMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SummonIntent() });
		MoveState val2 = new MoveState("FIREBALL", (Func<IReadOnlyList<Creature>, Task>)FireballMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(FireballDamage) });
		MoveState val3 = new MoveState("BUFF", (Func<IReadOnlyList<Creature>, Task>)BuffMove, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val4 = new MoveState("MEGA_DEBUFF", (Func<IReadOnlyList<Creature>, Task>)MegaDebuffMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val5 = new MoveState("REVIVE", (Func<IReadOnlyList<Creature>, Task>)ReviveMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SummonIntent() });
		ConditionalBranchState item = (ConditionalBranchState)(object)(val5.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)item);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		if (InitialSpawn)
		{
			TurnsTaken++;
			return "SPAWN";
		}
		TurnsTaken++;
		if (TurnsTaken >= 3 && !UltUsed)
		{
			return "MEGA_DEBUFF";
		}
		int num = rng.NextInt(100);
		if (num <= 25 && IsMinionDead() && !LastMove(stateMachine, "REVIVE"))
		{
			return "REVIVE";
		}
		if (num <= 70 && !LastTwoMoves(stateMachine, "FIREBALL"))
		{
			return "FIREBALL";
		}
		if (!LastMove(stateMachine, "BUFF"))
		{
			return "BUFF";
		}
		return "FIREBALL";
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

	private async Task SpawnMove(IReadOnlyList<Creature> targets)
	{
		InitialSpawn = false;
		List<string> slots = ((MonsterModel)this).CombatState.Encounter.Slots.Where((string s) => s.StartsWith("torch")).ToList();
		foreach (string slot in slots)
		{
			AFTPModAudio.Play("collector", "collector_summon");
			Creature summoned = await CreatureCmd.Add<TorchHead>(((MonsterModel)this).CombatState, slot);
			await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), summoned, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task FireballMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack((decimal)FireballDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/attack_fire", (string)null)
			.WithHitFx("vfx/vfx_fire_burst", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task BuffMove(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)BlockAmount, (ValueProp)8, (CardPlay)null, false);
		foreach (Creature teammate in from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
			where t.IsAlive
			select t)
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), teammate, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task MegaDebuffMove(IReadOnlyList<Creature> targets)
	{
		TalkCmd.Play(_megaDebuffDialog, ((MonsterModel)this).Creature, (VfxColor)9, (VfxDuration)4);
		AFTPModAudio.Play("collector", "collector_debuff");
		Creature target = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature val) => val.IsAlive));
		if (target != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature targetNode = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (targetNode != null)
			{
				CollectorCurseEffect curse = CollectorCurseEffect.Create(targetNode.VfxSpawnPosition);
				NCombatRoom instance2 = NCombatRoom.Instance;
				Node vfxContainer = (Node)(object)((instance2 != null) ? instance2.CombatVfxContainer : null);
				if (vfxContainer != null)
				{
					GodotTreeExtensions.AddChildSafely(vfxContainer, (Node)(object)curse);
				}
			}
		}
		await Cmd.Wait(2f, false);
		foreach (Creature t in targets.Where((Creature val) => val.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), t, (decimal)MegaDebuffAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), t, (decimal)MegaDebuffAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), t, (decimal)MegaDebuffAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
		UltUsed = true;
	}

	private async Task ReviveMove(IReadOnlyList<Creature> targets)
	{
		List<string> emptySlots = ((MonsterModel)this).CombatState.Encounter.Slots.Where((string s) => s.StartsWith("torch")).Where(delegate(string s)
		{
			HashSet<string> hashSet = (from t in ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)
				where t.IsAlive
				select t.SlotName).ToHashSet();
			return !hashSet.Contains(s);
		}).ToList();
		foreach (string slot in emptySlots)
		{
			AFTPModAudio.Play("collector", "collector_summon");
			Creature summoned = await CreatureCmd.Add<TorchHead>(((MonsterModel)this).CombatState, slot);
			await PowerCmd.Apply<MinionPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), summoned, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
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

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__1(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
