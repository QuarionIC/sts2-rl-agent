using System.Threading.Tasks;
using BaseLib.Extensions;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Powers;

public class DefensiveModePower : GuardianPowerModel
{
	public DefensiveModePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithPower<ThornsPower>(3m);
	}

	protected override async Task AfterApplied(PlayerChoiceContext ctx, Creature? applier, CardModel? cardSource)
	{
		if (((PowerModel)this).Owner.Player != null)
		{
			await GuardianCmd.EnterDefensiveMode(ctx, ((PowerModel)this).Owner.Player);
			await PowerCmd.Apply<ThornsPower>(ctx, ((PowerModel)this).Owner, DynamicVarSetExtensions.Power<ThornsPower>(((PowerModel)this).DynamicVars).BaseValue, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public override bool ShouldClearBlock(Creature creature)
	{
		return creature != ((PowerModel)this).Owner;
	}

	protected override async Task AfterRemoved(PlayerChoiceContext ctx, Creature oldOwner)
	{
		if (oldOwner.Player != null)
		{
			await GuardianCmd.LeaveDefensiveMode(ctx, oldOwner.Player);
			await PowerCmd.Apply<ThornsPower>(ctx, ((PowerModel)this).Owner, -DynamicVarSetExtensions.Power<ThornsPower>(((PowerModel)this).DynamicVars).BaseValue, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	protected override async Task AfterEnergyReset(PlayerChoiceContext ctx, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
