using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class SerpentIdol : SneckoCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	public SerpentIdol()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithCards(3, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = SneckoModel.GetCombatSneckoCards(((CardModel)this).Owner, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue).ToList();
		CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, false);
		if (val != null)
		{
			val.SetToFreeThisTurn();
			await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
