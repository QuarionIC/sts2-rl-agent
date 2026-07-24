using Downfall.DownfallCode.Patches;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Utils;

public static class CardOverlayHooks
{
	public static void Refresh(CardModel card)
	{
		NCard val = NCard.FindOnTable(card, (PileType?)null);
		if (val != null)
		{
			CardOverlayPatches.Sync(val);
		}
	}
}
