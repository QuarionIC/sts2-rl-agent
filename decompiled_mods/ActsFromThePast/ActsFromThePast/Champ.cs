using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Champ : CustomMonsterModel
{
	private const int DebuffAmount = 2;

	private const int ExecuteCount = 2;

	private const int ForgeThreshold = 2;

	private static readonly LocString _tauntLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog1");

	private static readonly LocString _tauntLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog2");

	private static readonly LocString _tauntLine3 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog3");

	private static readonly LocString _tauntLine4 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog4");

	private static readonly LocString _limitBreakLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.ANGER.dialog1");

	private static readonly LocString _limitBreakLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.ANGER.dialog2");

	private static readonly LocString _deathLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.EXECUTE.dialog1");

	private static readonly LocString _deathLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.EXECUTE.dialog2");

	private static readonly LocString _beltLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.beltDialog");

	private const string HEAVY_SLASH = "HEAVY_SLASH";

	private const string DEFENSIVE_STANCE = "DEFENSIVE_STANCE";

	private const string EXECUTE = "EXECUTE";

	private const string FACE_SLAP = "FACE_SLAP";

	private const string GLOAT = "GLOAT";

	private const string TAUNT = "TAUNT";

	private const string ANGER = "ANGER";

	private int _numTurns;

	private int _forgeTimes;

	private bool _thresholdReached;

	private bool _firstTurn;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 440, 420);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 440, 420);

	private int SlashDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 18, 16);

	private int ExecuteDamage => 10;

	private int SlapDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 14, 12);

	private int StrengthAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 4, 3);

	private int ForgeAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 7, 5);

	private int BlockAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 20, 15);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/champ/champ.tscn";

	private int NumTurns
	{
		get
		{
			return _numTurns;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_numTurns = value;
		}
	}

	private int ForgeTimes
	{
		get
		{
			return _forgeTimes;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_forgeTimes = value;
		}
	}

	private bool ThresholdReached
	{
		get
		{
			return _thresholdReached;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_thresholdReached = value;
		}
	}

	private bool FirstTurn
	{
		get
		{
			return _firstTurn;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_firstTurn = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_numTurns = 0;
		_forgeTimes = 0;
		_thresholdReached = false;
		_firstTurn = true;
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__1(creature);
		if (creature == ((MonsterModel)this).Creature)
		{
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)4, (ShakeDuration)3, -1f);
			}
			PlayDeathSfx();
		}
	}

	private void PlayDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(2);
		if (1 == 0)
		{
		}
		string text = ((num != 0) ? "champ_death_2" : "champ_death_1");
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("champ", soundName);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("HEAVY_SLASH", (Func<IReadOnlyList<Creature>, Task>)HeavySlash, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(SlashDamage) });
		MoveState val2 = new MoveState("DEFENSIVE_STANCE", (Func<IReadOnlyList<Creature>, Task>)DefensiveStance, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		});
		MoveState val3 = new MoveState("EXECUTE", (Func<IReadOnlyList<Creature>, Task>)Execute, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new MultiAttackIntent(ExecuteDamage, 2) });
		MoveState val4 = new MoveState("FACE_SLAP", (Func<IReadOnlyList<Creature>, Task>)FaceSlap, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(SlapDamage),
			(AbstractIntent)new DebuffIntent(false)
		});
		MoveState val5 = new MoveState("GLOAT", (Func<IReadOnlyList<Creature>, Task>)Gloat, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val6 = new MoveState("TAUNT", (Func<IReadOnlyList<Creature>, Task>)Taunt, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val7 = new MoveState("ANGER", (Func<IReadOnlyList<Creature>, Task>)Anger, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val7.FollowUpState = (val6.FollowUpState = (val5.FollowUpState = (val4.FollowUpState = (val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))))))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)val4);
		list.Add((MonsterState)(object)val5);
		list.Add((MonsterState)(object)val6);
		list.Add((MonsterState)(object)val7);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		NumTurns++;
		if (((MonsterModel)this).Creature.CurrentHp < ((MonsterModel)this).Creature.MaxHp / 2 && !ThresholdReached)
		{
			ThresholdReached = true;
			return "ANGER";
		}
		if (ThresholdReached && !LastMove(stateMachine, "EXECUTE") && !LastMoveBefore(stateMachine, "EXECUTE"))
		{
			LocString val = ((Rng.Chaotic.NextInt(2) == 0) ? _deathLine1 : _deathLine2);
			TalkCmd.Play(val, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
			return "EXECUTE";
		}
		if (NumTurns == 4 && !ThresholdReached)
		{
			NumTurns = 0;
			return "TAUNT";
		}
		int num = rng.NextInt(100);
		if (!LastMove(stateMachine, "DEFENSIVE_STANCE") && ForgeTimes < 2 && num < 30)
		{
			ForgeTimes++;
			return "DEFENSIVE_STANCE";
		}
		if (!LastMove(stateMachine, "GLOAT") && !LastMove(stateMachine, "DEFENSIVE_STANCE") && num < 30)
		{
			return "GLOAT";
		}
		if (!LastMove(stateMachine, "FACE_SLAP") && num < 55)
		{
			return "FACE_SLAP";
		}
		if (!LastMove(stateMachine, "HEAVY_SLASH"))
		{
			return "HEAVY_SLASH";
		}
		return "FACE_SLAP";
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

	private static bool LastMoveBefore(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task HeavySlash(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "HeavySlash", 0f);
		await Cmd.Wait(0.4f, false);
		await DamageCmd.Attack((decimal)SlashDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/ironclad/ironclad_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task DefensiveStance(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, (decimal)BlockAmount, (ValueProp)8, (CardPlay)null, false);
		await PowerCmd.Apply<MetallicizePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)ForgeAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Execute(IReadOnlyList<Creature> targets)
	{
		await JumpAnimation.Play(((MonsterModel)this).Creature);
		await Cmd.Wait(0.5f, false);
		for (int i = 0; i < 2; i++)
		{
			VfxCmd.PlayOnCreatures(targets.Where((Creature t) => t.IsAlive), "vfx/vfx_heavy_blunt");
			await DamageCmd.Attack((decimal)ExecuteDamage).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/characters/ironclad/ironclad_attack", (string)null)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute((PlayerChoiceContext)null);
		}
	}

	private async Task FaceSlap(IReadOnlyList<Creature> targets)
	{
		AFTPModAudio.Play("champ", "champ_slap");
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)SlapDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<FrailPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Gloat(IReadOnlyList<Creature> targets)
	{
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)StrengthAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Taunt(IReadOnlyList<Creature> targets)
	{
		LocString[] tauntLines = (LocString[])(object)new LocString[4] { _tauntLine1, _tauntLine2, _tauntLine3, _tauntLine4 };
		LocString taunt = tauntLines[Rng.Chaotic.NextInt(tauntLines.Length)];
		AFTPModAudio.Play("champ", "champ_taunt");
		TalkCmd.Play(taunt, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Anger(IReadOnlyList<Creature> targets)
	{
		LocString[] limitBreakLines = (LocString[])(object)new LocString[2] { _limitBreakLine1, _limitBreakLine2 };
		LocString line = limitBreakLines[Rng.Chaotic.NextInt(limitBreakLines.Length)];
		AFTPModAudio.Play("champ", "champ_charge");
		TalkCmd.Play(line, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)5);
		await Cmd.Wait(0.75f, false);
		List<PowerModel> debuffs = ((MonsterModel)this).Creature.Powers.Where((PowerModel p) => (int)p.Type == 2).ToList();
		foreach (PowerModel debuff in debuffs)
		{
			await PowerCmd.Remove(debuff);
		}
		await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)(StrengthAmount * 3), ((MonsterModel)this).Creature, (CardModel)null, false);
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
		val4.AddAnyState("HeavySlash", val2, (Func<bool>)null);
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
