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

public class BronzeSlime : SlimeModel
{
	private int _skipTurns;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[2]
	{
		(DynamicVar)new DamageVar(10m, (ValueProp)8),
		new DynamicVar("Sleep", 2m)
	});

	public override SlimeType SlimeType => SlimeType.Specialist;

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "idle", (string)null, false, "hit", false, (string)null, false, (string)null, false);
	}

	public override async Task Command(PlayerChoiceContext ctx)
	{
		if (_skipTurns > 0)
		{
			_skipTurns--;
			return;
		}
		await DamageCmd.Attack(((DynamicVar)base.DynamicVars.Damage).BaseValue).FromSlime(this).TargetingAllOpponents(((MonsterModel)this).CombatState)
			.Execute(ctx);
		_skipTurns = base.DynamicVars["Sleep"].IntValue;
	}
}
