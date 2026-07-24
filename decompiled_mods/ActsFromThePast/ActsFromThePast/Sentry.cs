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
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Sentry : CustomMonsterModel
{
	public const string BOLT = "BOLT";

	public const string BEAM = "BEAM";

	private bool _boltFirst;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 39, 38);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 45, 42);

	private int BeamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 9);

	private int DazedAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 3, 2);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/sentry/sentry.tscn";

	public bool BoltFirst
	{
		get
		{
			return _boltFirst;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_boltFirst = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BOLT", (Func<IReadOnlyList<Creature>, Task>)BoltMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StatusIntent(DazedAmount) });
		MoveState val2 = (MoveState)(object)(val.FollowUpState = (MonsterState)new MoveState("BEAM", (Func<IReadOnlyList<Creature>, Task>)BeamMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(BeamDamage) }));
		val2.FollowUpState = (MonsterState)(object)val;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		MoveState val4 = (BoltFirst ? val : val2);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val4);
	}

	private async Task BoltMove(IReadOnlyList<Creature> targets)
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "spaz1", 
			1 => "spaz2", 
			_ => "spaz3", 
		};
		if (1 == 0)
		{
		}
		string spazAnim = text;
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, spazAnim, 0f);
		AFTPModAudio.Play("general", "thunderclap");
		Vector2 sentryPos = Sts1VfxHelper.GetCreatureCenter(((MonsterModel)this).Creature);
		ShockWaveEffect.PlayRoyal(sentryPos);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
		}
		await Cmd.Wait(0.5f, false);
		await CardPileCmd.AddToCombatAndPreview<Dazed>((IEnumerable<Creature>)targets, (PileType)3, DazedAmount, (Player)null, (CardPilePosition)1);
	}

	private async Task BeamMove(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Attack", 0f);
		BorderFlashEffect.PlaySky();
		AFTPModAudio.Play("general", "magic_beam_short");
		Creature playerCreature = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature c) => c.Player != null));
		Vector2 playerPos = ((playerCreature != null) ? Sts1VfxHelper.GetCreatureCenter(playerCreature) : Vector2.Zero);
		Vector2 sentryPos = Sts1VfxHelper.GetCreatureCenter(((MonsterModel)this).Creature);
		SmallLaserEffect laser = SmallLaserEffect.Create(sentryPos, playerPos);
		Sts1VfxHelper.Play(laser);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack((decimal)BeamDamage).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("attack", false);
		AnimState val3 = new AnimState("spaz1", false);
		AnimState val4 = new AnimState("spaz2", false);
		AnimState val5 = new AnimState("spaz3", false);
		AnimState val6 = new AnimState("hit", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = val;
		val5.NextState = val;
		val6.NextState = val;
		CreatureAnimator val7 = new CreatureAnimator(val, controller);
		val7.AddAnyState("Attack", val2, (Func<bool>)null);
		val7.AddAnyState("spaz1", val3, (Func<bool>)null);
		val7.AddAnyState("spaz2", val4, (Func<bool>)null);
		val7.AddAnyState("spaz3", val5, (Func<bool>)null);
		val7.AddAnyState("Hit", val6, (Func<bool>)null);
		MegaAnimationState animationState = controller.GetAnimationState();
		animationState.SetTimeScale(2f);
		MegaTrackEntry current = animationState.GetCurrent(0);
		current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
		animationState.Update(0f);
		animationState.Apply(controller.GetSkeleton());
		return val7;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
