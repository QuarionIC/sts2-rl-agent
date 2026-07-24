using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Enchantments;

public class Temporal : DownfallEnchantmentModel<Guardian.GuardianCode.Core.Guardian>
{
	public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((EnchantmentModel)this).Card.Owner)
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await GuardianCmd.PutIntoStasis(((EnchantmentModel)this).Card, ctx, (AbstractModel)(object)this, silent: true);
			}
		}
	}
}
