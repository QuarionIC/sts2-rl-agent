using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class StoneOfNomakk : SlimeBossRelicModel
{
	public StoneOfNomakk()
		: base((RelicRarity)2)
	{
		WithPower<PotencyPower>(1, showTooltip: true);
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber == 1 && player == ((RelicModel)this).Owner)
		{
			return MyCommonActions.ApplySelf<PotencyPower>(ctx, (AbstractModel)(object)this);
		}
		return Task.CompletedTask;
	}
}
