using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Cards.Uncommon;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class SleevedAce : SneckoRelicModel
{
	public SleevedAce()
		: base((RelicRarity)3)
	{
		WithTip(new RelicTooltipSource(delegate
		{
			CardModel obj = ((CardModel)ModelDb.Card<MarkedCard>()).ToMutable();
			obj.UpgradeInternal();
			return HoverTipFactory.FromCard(obj, false);
		}));
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await DownfallCardCmd.GiveCard<MarkedCard>(player, (PileType)2, (CardPilePosition)1, upgraded: true, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<MarkedCard>?)null, (Player?)null);
			}
		}
	}
}
