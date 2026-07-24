using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinWizard : CustomMonsterModel
{
	private static readonly LocString _chargingDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.CHARGING.dialog");

	private static readonly LocString _ultimateDialog = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.ULTIMATE_BLAST.dialog");

	private const int ChargeLimit = 3;

	private const string CHARGING = "CHARGING";

	private const string ULTIMATE_BLAST = "ULTIMATE_BLAST";

	private int _currentCharge = 1;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 22, 21);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 26, 25);

	private int UltimateDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 30, 25);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_wizard/gremlin_wizard.tscn";

	private int CurrentCharge
	{
		get
		{
			return _currentCharge;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_currentCharge = value;
		}
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
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("CHARGING", (Func<IReadOnlyList<Creature>, Task>)Charging, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new UnknownIntent() });
		MoveState val2 = new MoveState("ULTIMATE_BLAST", (Func<IReadOnlyList<Creature>, Task>)UltimateBlast, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(UltimateDamage) });
		val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("AFTER_CHARGE", SelectAfterCharge);
		val2.FollowUpState = (MonsterState)(object)val2;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add(val.FollowUpState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private string SelectAfterCharge(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		return (CurrentCharge >= 3) ? "ULTIMATE_BLAST" : "CHARGING";
	}

	private Task Charging(IReadOnlyList<Creature> targets)
	{
		CurrentCharge++;
		if (CurrentCharge >= 3)
		{
			PlayRandomChargeSfx();
			TalkCmd.Play(_ultimateDialog, ((MonsterModel)this).Creature, (VfxColor)3, (VfxDuration)4);
		}
		return Task.CompletedTask;
	}

	private async Task UltimateBlast(IReadOnlyList<Creature> targets)
	{
		CurrentCharge = 0;
		await DamageCmd.Attack((decimal)UltimateDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_fire_burst", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private void PlayRandomChargeSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "gremlin_wizard_talk_2" : "gremlin_wizard_talk_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_wizard", soundName);
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		((MonsterModel)this).Creature.Died += OnDeath;
		GremlinLeaderHelper.SubscribeToLeaderDeath(((MonsterModel)this).Creature, (CombatState)((MonsterModel)this).CombatState);
	}

	private void OnDeath(Creature _)
	{
		((MonsterModel)this).Creature.Died -= OnDeath;
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
			0 => "gremlin_wizard_death_1", 
			1 => "gremlin_wizard_death_2", 
			_ => "gremlin_wizard_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("gremlin_wizard", soundName);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("animation", true);
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
