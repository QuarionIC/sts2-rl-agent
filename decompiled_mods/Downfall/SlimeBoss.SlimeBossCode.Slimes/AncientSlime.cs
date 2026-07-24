using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Slimes;

public class AncientSlime : SlimeModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[2]
	{
		(DynamicVar)new DamageVar(3m, (ValueProp)8),
		(DynamicVar)new CardsVar(1)
	});

	public override SlimeType SlimeType => SlimeType.Specialist;

	public override IEnumerable<IHoverTip> ExtraTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<PotencyPower>((int?)null));

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "idle", (string)null, false, "hit", false, (string)null, false, (string)null, false);
	}

	public override async Task Command(PlayerChoiceContext ctx)
	{
		await DamageCmd.Attack(((DynamicVar)base.DynamicVars.Damage).BaseValue).FromSlime(this).TargetingRandomOpponents(((MonsterModel)this).CombatState, true)
			.Execute(ctx);
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (!((MonsterModel)this).Creature.IsAlive || player != ((MonsterModel)this).Creature.PetOwner)
		{
			return count;
		}
		return count + (decimal)((DynamicVar)base.DynamicVars.Cards).IntValue;
	}
}
