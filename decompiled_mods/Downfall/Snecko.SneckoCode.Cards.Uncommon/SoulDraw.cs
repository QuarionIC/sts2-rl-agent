using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class SoulDraw : SneckoCardModel
{
	public SoulDraw()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = SneckoModel.GetCombatSneckoCards(((CardModel)this).Owner, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue).ToList();
		list.ForEach(delegate(CardModel card)
		{
			card.AddKeyword((CardKeyword)5);
		});
		await CardPileCmd.Add((IEnumerable<CardModel>)list, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
