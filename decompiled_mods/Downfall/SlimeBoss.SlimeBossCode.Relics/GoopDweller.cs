using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class GoopDweller : SlimeBossRelicModel
{
	public GoopDweller()
		: base((RelicRarity)2)
	{
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber == 1 && player == ((RelicModel)this).Owner)
		{
			await SlimeBossCmd.Split<BruiserSlime>(ctx, player);
		}
	}
}
