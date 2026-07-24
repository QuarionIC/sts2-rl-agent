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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Transient : CustomMonsterModel
{
	private const int IncrementDmg = 10;

	private const string ATTACK = "ATTACK";

	private int _count = 0;

	private decimal _multiplayerDamageMultiplier = 1m;

	public override int MinInitialHp => 999;

	public override int MaxInitialHp => 999;

	private int StartingDeathDmg => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 40, 30);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/transient/transient.tscn";

	private int Count
	{
		get
		{
			return _count;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_count = value;
		}
	}

	private int CurrentAttackDamage => (int)((decimal)(StartingDeathDmg + Count * 10) * _multiplayerDamageMultiplier);

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		await PowerCmd.Apply<FadingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)AscensionHelper.GetValueIfAscension((AscensionLevel)8, 6, 5), ((MonsterModel)this).Creature, (CardModel)null, false);
		await PowerCmd.Apply<ShiftingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		int playerCount = ((MonsterModel)this).Creature.CombatState.Players.Count;
		if (playerCount > 1)
		{
			_multiplayerDamageMultiplier = (decimal)playerCount * MultiplayerScalingModel.GetMultiplayerScaling(((MonsterModel)this).Creature.CombatState.Encounter, ((MonsterModel)this).Creature.CombatState.RunState.CurrentActIndex);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		MoveState val = new MoveState("ATTACK", (Func<IReadOnlyList<Creature>, Task>)Attack, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicSingleAttackIntent(() => CurrentAttackDamage) });
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { (MonsterState)(object)val }, (MonsterState)(object)val);
	}

	private async Task Attack(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Swing", 0.4f);
		await DamageCmd.Attack((decimal)CurrentAttackDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_starry_impact", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		Count++;
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
		AnimState val3 = new AnimState("Hurt", false);
		val2.NextState = val;
		val3.NextState = val;
		CreatureAnimator val4 = new CreatureAnimator(val, controller);
		val4.AddAnyState("Swing", val2, (Func<bool>)null);
		val4.AddAnyState("Hurt", val3, (Func<bool>)null);
		return val4;
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
