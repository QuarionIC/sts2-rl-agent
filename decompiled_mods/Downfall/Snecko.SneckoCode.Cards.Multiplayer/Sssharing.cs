using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Multiplayer;

[Pool(typeof(SneckoCardPool))]
public class Sssharing : SneckoCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Sssharing()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)7)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int amount = ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue;
		foreach (Player player in ((IPlayerCollection)((CardModel)this).Owner.RunState).Players)
		{
			List<CardModel> list = SneckoModel.GetCombatSneckoCards(((CardModel)this).Owner, amount, player).ToList();
			foreach (CardModel item in list)
			{
				item.SetToFreeThisTurn();
			}
			await CardPileCmd.Add((IEnumerable<CardModel>)list, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
