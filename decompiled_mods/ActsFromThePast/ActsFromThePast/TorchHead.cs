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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public sealed class TorchHead : CustomMonsterModel
{
	private const int AttackDamage = 7;

	private const float FireInterval = 0.04f;

	private const string TACKLE = "TACKLE";

	private SceneTreeTimer _fireTimer;

	private bool _alive = true;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 40, 38);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 45, 40);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/torch_head/torch_head.tscn";

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
		object obj3 = obj2.GetType().GetMethod("FindBone")?.Invoke(obj2, new object[1] { "fireslot" });
		if (obj3 == null)
		{
			return;
		}
		object? obj4 = obj3.GetType().GetProperty("BoundObject")?.GetValue(obj3);
		GodotObject val3 = (GodotObject)((obj4 is GodotObject) ? obj4 : null);
		if (val3 != null)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val4 = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val4 != null)
			{
				SpawnFireParticle(val, val3, val4);
			}
		}
	}

	private void SpawnFireParticle(object creatureNode, GodotObject spineBone, SceneTree tree)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		if (!_alive)
		{
			return;
		}
		try
		{
			float num = (float)spineBone.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
			float num2 = (float)spineBone.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
			dynamic val = ((dynamic)creatureNode).GlobalPosition;
			Vector2 position = new Vector2(val.X + num * 1.1f, val.Y + num2 * 1.1f - 20f);
			TorchHeadFireEffect torchHeadFireEffect = TorchHeadFireEffect.Create(position);
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				Control combatVfxContainer = instance.CombatVfxContainer;
				if (combatVfxContainer != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)torchHeadFireEffect);
				}
			}
			_fireTimer = tree.CreateTimer(0.03999999910593033, true, false, false);
			((GodotObject)_fireTimer).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
			{
				SpawnFireParticle(creatureNode, spineBone, tree);
			}), 0u);
		}
		catch (Exception ex)
		{
			Log.Info("[TorchHead] Fire particle error: " + ex.Message, 2);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("TACKLE", (Func<IReadOnlyList<Creature>, Task>)TackleMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(7) });
		val.FollowUpState = (MonsterState)(object)val;
		list.Add((MonsterState)(object)val);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private async Task TackleMove(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack(7m).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/axe_ruby_raider/axe_ruby_raider_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
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
