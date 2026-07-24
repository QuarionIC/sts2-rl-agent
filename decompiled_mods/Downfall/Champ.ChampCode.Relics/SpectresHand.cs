using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Cards;
using Champ.ChampCode.Cards.Basic;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Events;
using Champ.ChampCode.Stance;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class SpectresHand : ChampRelicModel, IOnChampStanceChange
{
	public SpectresHand()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		WithTip(ChampTip.Stance);
		WithTip(DownfallKeyword.Echo);
	}

	public async Task OnChampStanceChange(PlayerChoiceContext ctx, Player player, ChampStanceModel oldStance, ChampStanceModel newStance)
	{
		if (player == ((RelicModel)this).Owner && !(newStance is ChampNoStance) && oldStance != newStance)
		{
			CardModel val = (CardModel)(object)((!player.RunState.Rng.CombatCardGeneration.NextBool()) ? ((ChampCardModel)(await DownfallCardCmd.GiveCard<DefendChamp>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<DefendChamp>?)null, (Player?)null))) : ((ChampCardModel)(await DownfallCardCmd.GiveCard<StrikeChamp>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<StrikeChamp>?)null, (Player?)null))));
			val.ToEcho();
			val.SetToFreeThisTurn();
		}
	}
}
