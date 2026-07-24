using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.DynamicVars;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Slimes;

public class TimeSlime : SlimeModel
{
	public override SlimeType SlimeType => SlimeType.Specialist;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[2]
	{
		(DynamicVar)new DamageVar(4m, (ValueProp)8),
		new SlimeSecondaryVar(1m)
	});

	public override IEnumerable<IHoverTip> ExtraTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<WeakPower>((int?)null));

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "idle", (string)null, false, "hit", false, (string)null, false, (string)null, false);
	}

	public override async Task Command(PlayerChoiceContext ctx)
	{
		IEnumerable<Creature> enumerable = from e in (await DamageCmd.Attack(((DynamicVar)base.DynamicVars.Damage).BaseValue).FromSlime(this).TargetingRandomOpponents(((MonsterModel)this).CombatState, true)
				.Execute(ctx)).Results.SelectMany((List<DamageResult> e) => e)
			select e.Receiver;
		int intValue = ((DynamicVar)base.DynamicVars.Slime()).IntValue;
		IEnumerable<IModifySecondarySlimeEffects> modifiers;
		int num = SlimeBossHook.ModifySecondarySlimeEffects(((MonsterModel)this).CombatState, intValue, out modifiers, this);
		await PowerCmd.Apply<WeakPower>(ctx, enumerable, (decimal)num, ((MonsterModel)this).Creature, (CardModel)null, false);
	}
}
