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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Cultist : CustomMonsterModel
{
	private static readonly LocString _incantationLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.moves.INCANTATION.speakLine1");

	private static readonly LocString _incantationLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.moves.INCANTATION.speakLine2");

	private static readonly LocString _deathLine = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.deathLine");

	private bool _saidPower;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 50, 48);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 56, 54);

	private int AttackDamage => 6;

	private int RitualAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 5, 3);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/cultist/cultist.tscn";

	private bool SaidPower
	{
		get
		{
			return _saidPower;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_saidPower = value;
		}
	}

	public override async Task BeforeDeath(Creature creature)
	{
		await _003C_003En__0(creature);
		if (creature != ((MonsterModel)this).Creature)
		{
			return;
		}
		PlayDeathSfx();
		if (!SaidPower)
		{
			return;
		}
		string text = _deathLine.GetFormattedText();
		NSpeechBubbleVfx bubble = NSpeechBubbleVfx.Create(text, ((MonsterModel)this).Creature, 2.5, (VfxColor)5);
		if (bubble != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)bubble);
			}
			await Cmd.Wait(2.5f, false);
		}
	}

	private void PlayDeathSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "cultist_death_1", 
			1 => "cultist_death_2", 
			_ => "cultist_death_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("cultist", soundName);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		MoveState val = new MoveState("INCANTATION", (Func<IReadOnlyList<Creature>, Task>)Incantation, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new BuffIntent() });
		MoveState val2 = (MoveState)(object)(val.FollowUpState = (MonsterState)new MoveState("DARK_STRIKE", (Func<IReadOnlyList<Creature>, Task>)DarkStrike, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(AttackDamage) }));
		val2.FollowUpState = (MonsterState)(object)val2;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2
		}, (MonsterState)(object)val);
	}

	private async Task Incantation(IReadOnlyList<Creature> targets)
	{
		PlayIncantationSfx();
		ICombatState combatState = ((MonsterModel)this).Creature.CombatState;
		Player player = ((combatState != null) ? combatState.Players.FirstOrDefault() : null);
		if (((player != null) ? player.RunState.CurrentActIndex : (-1)) == 0)
		{
			int roll = Rng.Chaotic.NextInt(10);
			if (roll < 3)
			{
				TalkCmd.Play(_incantationLine1, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
				SaidPower = true;
			}
			else if (roll < 6)
			{
				TalkCmd.Play(_incantationLine2, ((MonsterModel)this).Creature, (VfxColor)2, (VfxDuration)4);
			}
		}
		await Cmd.Wait(0.5f, false);
		await PowerCmd.Apply<RitualPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)RitualAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private void PlayIncantationSfx()
	{
		int num = Rng.Chaotic.NextInt(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "cultist_talk_1", 
			1 => "cultist_talk_2", 
			_ => "cultist_talk_3", 
		};
		if (1 == 0)
		{
		}
		string soundName = text;
		AFTPModAudio.Play("cultist", soundName);
	}

	private async Task DarkStrike(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)AttackDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		AnimState val = new AnimState("waving", true);
		return new CreatureAnimator(val, controller);
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0(Creature creature)
	{
		return ((AbstractModel)this).BeforeDeath(creature);
	}
}
