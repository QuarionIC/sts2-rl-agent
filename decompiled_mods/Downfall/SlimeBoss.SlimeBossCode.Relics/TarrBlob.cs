using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class TarrBlob : SlimeBossRelicModel
{
	public TarrBlob()
		: base((RelicRarity)7)
	{
		WithEnergy(1);
		WithVar("Decrease", 1);
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return amount;
		}
		return amount + ((DynamicVar)((RelicModel)this).DynamicVars.Energy).BaseValue;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber == 1 && player == ((RelicModel)this).Owner)
		{
			await SlimeBossCmd.DecreaseSlots(ctx, player, ((RelicModel)this).DynamicVars["Decrease"].IntValue);
		}
	}
}
