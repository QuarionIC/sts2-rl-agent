using System;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Cards.Curse;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Relics;

public sealed class OldLocket : HermitRelicModel
{
	public OldLocket()
		: base((RelicRarity)1)
	{
		WithTips((RelicModel e) => HoverTipFactory.FromCardWithCardHoverTips<MementoCard>(false));
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<ClaspedLocket>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber == 1 && player == ((RelicModel)this).Owner)
		{
			await DownfallCardCmd.GiveCard<MementoCard>(((RelicModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<MementoCard>?)null, (Player?)null);
			((RelicModel)this).Flash();
		}
	}
}
