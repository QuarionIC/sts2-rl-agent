using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Slimes;

public class TorchheadSlime : SlimeModel
{
	public override SlimeType SlimeType => SlimeType.Specialist;

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)new DamageVar(6m, (ValueProp)8));

	public override IEnumerable<IHoverTip> ExtraTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<StrengthPower>((int?)null));

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "idle", (string)null, false, "hit", false, (string)null, false, (string)null, false);
	}

	public override async Task Command(PlayerChoiceContext ctx)
	{
		await DamageCmd.Attack(((DynamicVar)base.DynamicVars.Damage).BaseValue + (decimal)base.PetOwner.GetPowerAmount<StrengthPower>()).FromSlime(this).TargetingRandomOpponents(((MonsterModel)this).CombatState, true)
			.Execute(ctx);
	}
}
