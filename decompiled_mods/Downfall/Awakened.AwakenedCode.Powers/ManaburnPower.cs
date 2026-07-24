using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using Downfall.DownfallCode.Extensions.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class ManaburnPower : AwakenedPowerModel, IOnDrained
{
	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	public ManaburnPower()
		: base((PowerType)2, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		WithTip(AwakenedTip.Drained.WithVars((DynamicVar)new EnergyVar(1)));
	}

	public async Task OnDrained(PlayerChoiceContext ctx, Player player, int amount)
	{
		if (((PowerModel)this).Applier == player.Creature && LocalContext.NetId.HasValue)
		{
			int num = ((PowerModel)this).Amount * amount;
			IEnumerable<IModifyManaburnDamage> modifiers;
			decimal modifiedAmount = AwakenedHook.ModifyManaburnDamage(((PowerModel)this).CombatState, num, player, out modifiers);
			await AwakenedHook.AfterModifyingManaburnDamage(((PowerModel)this).CombatState, ctx, player, modifiers);
			await CreatureCmd.Damage(ctx, ((PowerModel)this).Owner, modifiedAmount, (ValueProp)14, player.Creature);
		}
	}
}
