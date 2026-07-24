using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Slimes;

public class GuerillaSlime : SlimeModel
{
	public override SlimeType SlimeType => SlimeType.Normal;

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)new DamageVar(3m, (ValueProp)8));

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "idle", (string)null, false, "damage", false, (string)null, false, (string)null, false);
	}

	public override async Task Command(PlayerChoiceContext ctx)
	{
		await DamageCmd.Attack(((DynamicVar)base.DynamicVars.Damage).BaseValue).FromSlime(this).TargetingAllOpponents(((MonsterModel)this).CombatState)
			.Execute(ctx);
	}
}
