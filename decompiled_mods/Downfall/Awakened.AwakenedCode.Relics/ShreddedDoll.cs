using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class ShreddedDoll : AwakenedRelicModel
{
	public ShreddedDoll()
		: base((RelicRarity)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AwakenedTip.Conjure);
		WithPower<RitualPower>(1, showTooltip: true);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await MyCommonActions.ApplySelf<RitualPower>(ctx, (AbstractModel)(object)this);
			}
			((RelicModel)this).Flash();
			await AwakenedCmd.Conjure(((RelicModel)this).Owner);
		}
	}
}
