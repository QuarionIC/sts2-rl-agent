using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Bear : CustomMonsterModel
{
	private const int LungeBlock = 9;

	private const string MAUL = "MAUL";

	private const string BEAR_HUG = "BEAR_HUG";

	private const string LUNGE = "LUNGE";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 40, 38);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 44, 42);

	private int MaulDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 20, 18);

	private int LungeDamage => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 10, 9);

	private int DexReduction => AscensionHelper.GetValueIfAscension((AscensionLevel)9, 4, 2);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/bear/bear.tscn";

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BEAR_HUG", (Func<IReadOnlyList<Creature>, Task>)BearHug, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		MoveState val2 = new MoveState("MAUL", (Func<IReadOnlyList<Creature>, Task>)Maul, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(MaulDamage) });
		MoveState val3 = (MoveState)(object)(val.FollowUpState = (MonsterState)new MoveState("LUNGE", (Func<IReadOnlyList<Creature>, Task>)Lunge, (AbstractIntent[])(object)new AbstractIntent[2]
		{
			(AbstractIntent)new SingleAttackIntent(LungeDamage),
			(AbstractIntent)new DefendIntent()
		}));
		val3.FollowUpState = (MonsterState)(object)val2;
		val2.FollowUpState = (MonsterState)(object)val3;
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val);
	}

	private async Task BearHug(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, (decimal)(-DexReduction), ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	private async Task Maul(IReadOnlyList<Creature> targets)
	{
		await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "Maul", 0f);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack((decimal)MaulDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_blunt", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
	}

	private async Task Lunge(IReadOnlyList<Creature> targets)
	{
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)LungeDamage).FromMonster((MonsterModel)(object)this).WithHitFx("vfx/vfx_attack_slash", (string)null, "blunt_attack.mp3")
			.Execute((PlayerChoiceContext)null);
		await CreatureCmd.GainBlock(((MonsterModel)this).Creature, 9m, (ValueProp)8, (CardPlay)null, false);
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
		val4.AddAnyState("Maul", val2, (Func<bool>)null);
		val4.AddAnyState("Hit", val3, (Func<bool>)null);
		return val4;
	}
}
