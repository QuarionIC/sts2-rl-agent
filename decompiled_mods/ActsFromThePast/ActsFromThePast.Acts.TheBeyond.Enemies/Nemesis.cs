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
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Nemesis : CustomMonsterModel
{
	private const int ScytheDamage = 45;

	private const int FireTimes = 3;

	private const int BurnAmount = 5;

	private const int ScytheCooldownTurns = 2;

	private const string TRI_ATTACK = "TRI_ATTACK";

	private const string SCYTHE = "SCYTHE";

	private const string TRI_BURN = "TRI_BURN";

	private bool _firstMove = true;

	private int _scytheCooldown = 0;

	private bool _alive = true;

	private SceneTreeTimer _fireTimer;

	private Tween _opacityTween;

	private bool _shouldApplyIntangible;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 200, 185);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 200, 185);

	private int FireDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 7, 6);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/nemesis/nemesis.tscn";

	private bool ShouldApplyIntangible
	{
		get
		{
			return _shouldApplyIntangible;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_shouldApplyIntangible = value;
		}
	}

	private bool FirstMove
	{
		get
		{
			return _firstMove;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_firstMove = value;
		}
	}

	private int ScytheCooldown
	{
		get
		{
			return _scytheCooldown;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_scytheCooldown = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
		StartFireLoop();
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		_alive = false;
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
		if (obj2 == null)
		{
			return;
		}
		object obj3 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "eye0" });
		object obj4 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "eye1" });
		object obj5 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "eye2" });
		object? obj6 = obj3?.GetType().GetProperty("BoundObject")?.GetValue(obj3);
		GodotObject val3 = (GodotObject)((obj6 is GodotObject) ? obj6 : null);
		object? obj7 = obj4?.GetType().GetProperty("BoundObject")?.GetValue(obj4);
		GodotObject val4 = (GodotObject)((obj7 is GodotObject) ? obj7 : null);
		object? obj8 = obj5?.GetType().GetProperty("BoundObject")?.GetValue(obj5);
		GodotObject val5 = (GodotObject)((obj8 is GodotObject) ? obj8 : null);
		GodotObject[] array = ((IEnumerable<GodotObject>)(object)new GodotObject[3] { val3, val4, val5 }).Where((GodotObject b) => b != null).ToArray();
		if (array.Length != 0)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val6 = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val6 != null)
			{
				SpawnFireParticles(val, array, val6);
			}
		}
	}

	private void SpawnFireParticles(object creatureNode, GodotObject[] bones, SceneTree tree)
	{
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		if (!_alive)
		{
			return;
		}
		try
		{
			dynamic val = ((dynamic)creatureNode).GlobalPosition;
			GodotObject[] array = bones;
			foreach (GodotObject val2 in array)
			{
				float num = (float)val2.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
				float num2 = (float)val2.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
				Vector2 position = new Vector2(val.X + num * 1.1f + 10f, val.Y + num2 * 1.1f + 10f);
				NemesisFireParticle nemesisFireParticle = NemesisFireParticle.Create(position);
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					Control combatVfxContainer = instance.CombatVfxContainer;
					if (combatVfxContainer != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)nemesisFireParticle);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Info("[Nemesis] Fire particle error: " + ex.Message, 2);
		}
		_fireTimer = tree.CreateTimer(0.05000000074505806, true, false, false);
		((GodotObject)_fireTimer).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
		{
			SpawnFireParticles(creatureNode, bones, tree);
		}), 0u);
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power is IntangiblePower && power.Owner == ((MonsterModel)this).Creature)
		{
			float targetAlpha = ((amount > 0m) ? 0.5f : 1f);
			UpdateOpacity(targetAlpha);
		}
		return Task.CompletedTask;
	}

	private void UpdateOpacity(float targetAlpha)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
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
			obj = ((visuals != null) ? visuals.GetCurrentBody() : null);
		}
		Node2D val2 = (Node2D)obj;
		if (val2 != null)
		{
			Tween opacityTween = _opacityTween;
			if (opacityTween != null)
			{
				opacityTween.Kill();
			}
			_opacityTween = ((Node)val).CreateTween();
			Color modulate = ((CanvasItem)val2).Modulate;
			_opacityTween.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate"), Variant.op_Implicit(new Color(modulate.R, modulate.G, modulate.B, targetAlpha)), 0.3499999940395355).SetEase((EaseType)2).SetTrans((TransitionType)1);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		MoveState val = new MoveState("TRI_ATTACK", (Func<IReadOnlyList<Creature>, Task>)TriAttack, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(FireDamage, 3) });
		MoveState val2 = new MoveState("SCYTHE", (Func<IReadOnlyList<Creature>, Task>)Scythe, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(45) });
		MoveState val3 = new MoveState("TRI_BURN", (Func<IReadOnlyList<Creature>, Task>)TriBurn, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StatusIntent(5) });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2,
			(MonsterState)(object)val3,
			(MonsterState)(object)conditionalBranchState
		}, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		ScytheCooldown--;
		if (FirstMove)
		{
			FirstMove = false;
			return (rng.NextInt(100) < 50) ? "TRI_ATTACK" : "TRI_BURN";
		}
		int num = rng.NextInt(100);
		if (num < 30)
		{
			if (!LastMove(stateMachine, "SCYTHE") && ScytheCooldown <= 0)
			{
				ScytheCooldown = 2;
				return "SCYTHE";
			}
			if (rng.NextFloat(1f) < 0.5f)
			{
				if (!LastTwoMoves(stateMachine, "TRI_ATTACK"))
				{
					return "TRI_ATTACK";
				}
				return "TRI_BURN";
			}
			if (!LastMove(stateMachine, "TRI_BURN"))
			{
				return "TRI_BURN";
			}
			return "TRI_ATTACK";
		}
		if (num < 65)
		{
			if (!LastTwoMoves(stateMachine, "TRI_ATTACK"))
			{
				return "TRI_ATTACK";
			}
			if (rng.NextFloat(1f) < 0.5f)
			{
				if (ScytheCooldown <= 0)
				{
					ScytheCooldown = 2;
					return "SCYTHE";
				}
				return "TRI_BURN";
			}
			return "TRI_BURN";
		}
		if (!LastMove(stateMachine, "TRI_BURN"))
		{
			return "TRI_BURN";
		}
		if (rng.NextFloat(1f) < 0.5f && ScytheCooldown <= 0)
		{
			ScytheCooldown = 2;
			return "SCYTHE";
		}
		return "TRI_ATTACK";
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

	private async Task TriAttack(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 3; i++)
		{
			await DamageCmd.Attack((decimal)FireDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/necrobinder/necrobinder_attack", (string)null)
				.WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task Scythe(IReadOnlyList<Creature> targets)
	{
		PlayScytheSfx();
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Slash", 0.4f);
		await DamageCmd.Attack(45m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/necrobinder/necrobinder_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task TriBurn(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("nemesis", "nemesis_talk_3");
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (creatureNode != null)
		{
			ShockWaveEffect.PlayChaotic(creatureNode.VfxSpawnPosition);
		}
		await Cmd.Wait(1.5f, false);
		ICombatState combatState = ((MonsterModel)this).Creature.CombatState;
		Player player = ((combatState != null) ? combatState.Players.FirstOrDefault() : null);
		if (player != null)
		{
			List<CardPileAddResult> results = new List<CardPileAddResult>();
			await CardPileCmd.AddToCombatAndPreview<Burn>((IEnumerable<Creature>)targets, (PileType)3, 5, (Player)null, (CardPilePosition)1);
			CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)results, 2f, (CardPreviewStyle)1);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (participants.Contains(((MonsterModel)this).Creature))
		{
			ShouldApplyIntangible = !ShouldApplyIntangible;
			if (ShouldApplyIntangible)
			{
				await PowerCmd.Apply<IntangiblePower>(choiceContext, ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
			else if (((MonsterModel)this).Creature.HasPower<IntangiblePower>())
			{
				await PowerCmd.Remove((PowerModel)(object)((MonsterModel)this).Creature.GetPower<IntangiblePower>());
			}
		}
	}

	private void PlayScytheSfx()
	{
		string soundName = ((Rng.Chaotic.NextInt(2) == 0) ? "nemesis_talk_1" : "nemesis_talk_2");
		AFTPModAudio.Play("nemesis", soundName);
	}

	private void PlayDeathSfx()
	{
		string soundName = ((Rng.Chaotic.NextInt(2) == 0) ? "nemesis_death_1" : "nemesis_death_2");
		AFTPModAudio.Play("nemesis", soundName);
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			PlayDeathSfx();
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
		controller.GetAnimationState().SetTimeScale(0.8f);
		return val4;
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
