using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Cards.Token;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class SuperSneckoSoul : SneckoRelicModel
{
	public SuperSneckoSoul()
		: base((RelicRarity)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Muddle);
		WithCards(1);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner && player.PlayerCombatState != null)
		{
			if (player.PlayerCombatState.TurnNumber == 1)
			{
				await DownfallCardCmd.GiveCard<SoulRoll>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<SoulRoll>?)null, (Player?)null);
			}
			CardModel val = await CardPileCmd.Draw(ctx, ((RelicModel)this).Owner);
			if (val != null)
			{
				await SneckoCmd.Muddle(ctx, val, (AbstractModel?)(object)this);
			}
		}
	}
}
