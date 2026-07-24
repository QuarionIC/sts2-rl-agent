using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Deca : CustomMonsterModel
{
	private const int BeamCount = 2;

	private const int BeamDazeAmount = 2;

	private const int ProtectBlock = 16;

	private const int ProtectPlatedArmorAmount = 3;

	private const string BEAM = "BEAM";

	private const string SQUARE_OF_PROTECTION = "SQUARE_OF_PROTECTION";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 265, 250);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 265, 250);

	private int BeamDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 12, 10);

	private int ArtifactAmount => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 3, 2);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/deca/deca.tscn";

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)ArtifactAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		MoveState val = new MoveState("BEAM", (Func<IReadOnlyList<Creature>, Task>)Beam, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new MultiAttackIntent(BeamDamage, 2),
			(AbstractIntent)new StatusIntent(2)
		});
		MoveState val2 = (MoveState)(object)(val.FollowUpState = (MonsterState)new MoveState("SQUARE_OF_PROTECTION", (Func<IReadOnlyList<Creature>, Task>)SquareOfProtection, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new DefendIntent(),
			(AbstractIntent)new BuffIntent()
		}));
		val2.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState>
		{
			(MonsterState)(object)val,
			(MonsterState)(object)val2
		}, (MonsterState)(object)val);
	}

	private async Task Beam(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Beam", 0f);
		await Cmd.Wait(0.5f, false);
		await DamageCmd.Attack((decimal)BeamDamage).FromMonster((MonsterModel)(object)this).WithHitCount(2)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		await CardPileCmd.AddToCombatAndPreview<Dazed>((IEnumerable<Creature>)targets, (PileType)3, 2, (Player)null, (CardPilePosition)1);
	}

	private async Task SquareOfProtection(IReadOnlyList<Creature> targets)
	{
		IReadOnlyList<Creature> teammates = ((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature);
		foreach (Creature teammate in teammates)
		{
			if (teammate.IsAlive)
			{
				await CreatureCmd.GainBlock(teammate, 16m, (ValueProp)8, (CardPlay)null, false);
				await PowerCmd.Apply<PlatedArmorPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), teammate, 3m, ((MonsterModel)this).Creature, (CardModel)null, false);
			}
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
		AnimState val2 = new AnimState("Attack_2", false);
		AnimState val3 = new AnimState("Hit", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("Beam", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
