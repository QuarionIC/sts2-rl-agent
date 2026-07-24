using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class CryoChamber : GuardianRelicModel, IBeforeCardEntersStasis
{
	public CryoChamber()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.Stasis);
	}

	public Task BeforeCardEntersStasis(PlayerChoiceContext ctx, CardModel card, AbstractModel source)
	{
		if (card.Owner != ((RelicModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		CardCmd.Upgrade(card, (CardPreviewStyle)1);
		return Task.CompletedTask;
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				GuardianCmd.AddMaxStasisSlots(player);
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}
}
