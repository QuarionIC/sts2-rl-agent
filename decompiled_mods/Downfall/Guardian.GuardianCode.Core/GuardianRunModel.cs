using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Interfaces;
using Guardian.GuardianCode.RestSiteOptions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Core;

public class GuardianRunModel : CustomSingletonModel
{
	public GuardianRunModel()
		: base((HookType)2)
	{
	}

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (options.Any((RestSiteOption option) => option.OptionId == "DOWNFALL-GEM"))
		{
			return false;
		}
		IReadOnlyList<CardModel> deck = player.GetDeck();
		bool flag = deck.Any((CardModel e) => e is IGemCard);
		if (!deck.Any((CardModel e) => e is IGemSocketCard gemSocketCard && gemSocketCard.FreeSlots > 0) || !flag)
		{
			return false;
		}
		options.Add((RestSiteOption)(object)new GemRestSiteOption(player));
		return true;
	}
}
