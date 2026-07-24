using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Powers;

public class UnendingSupplyPower : SneckoPowerModel
{
	public UnendingSupplyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		WithTip(DownfallKeyword.Echo);
		WithTip((CardKeyword)2);
		WithTip((CardKeyword)1);
		WithTip(SneckoTip.Offclass);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (((PowerModel)this).Owner != player.Creature)
		{
			return;
		}
		List<CardModel> list = SneckoModel.GetCombatSneckoCards(player, ((PowerModel)this).Amount).ToList();
		foreach (CardModel item in list)
		{
			item.ToEcho();
		}
		await CardPileCmd.Add((IEnumerable<CardModel>)list, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
