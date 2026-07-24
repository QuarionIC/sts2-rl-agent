using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinMad : CustomMonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 21, 20);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 25, 24);

	private int ScratchDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 4);

	private int AngryAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 2, 1);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_mad/gremlin_mad.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<AngryPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)AngryAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
		((MonsterModel)this).Creature.Died += OnDeath;
		GremlinLeaderHelper.SubscribeToLeaderDeath(((MonsterModel)this).Creature, (CombatState)((MonsterModel)this).CombatState);
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
		PlayRandomDeathSfx();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		MoveState val = new MoveState("SCRATCH", (Func<IReadOnlyList<Creature>, Task>)Scratch, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(ScratchDamage) });
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { (MonsterState)(object)val }, (MonsterState)(object)val);
	}

	private async Task Scratch(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)ScratchDamage).FromMonster((MonsterModel)(object)this).WithHitFx((string)null, (string)null, "slash_attack.mp3")
			.WithHitVfxNode((Func<Creature, Node2D>)delegate(Creature target)
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_005d: Unknown result type (might be due to invalid IL or missing references)
				//IL_007a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0071: Unknown result type (might be due to invalid IL or missing references)
				Node2D val = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_scratch")).Instantiate<Node2D>((GenEditState)0);
				val.Scale = new Vector2(-1f, 1f);
				NCombatRoom instance = NCombatRoom.Instance;
				Vector2? obj;
				if (instance == null)
				{
					obj = null;
				}
				else
				{
					NCreature creatureNode = instance.GetCreatureNode(target);
					obj = ((creatureNode != null) ? new Vector2?(creatureNode.VfxSpawnPosition) : ((Vector2?)null));
				}
				val.GlobalPosition = (Vector2)(((_003F?)obj) ?? Vector2.Zero);
				return val;
			})
			.Execute((PlayerChoiceContext)null);
	}

	private void PlayRandomDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "gremlin_mad_death_2" : "gremlin_mad_death_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_mad", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		CreatureAnimator result = new CreatureAnimator(val, controller);
		MegaAnimationState animationState = controller.GetAnimationState();
		MegaTrackEntry current = animationState.GetCurrent(0);
		current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
		animationState.Update(0f);
		animationState.Apply(controller.GetSkeleton());
		return result;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
