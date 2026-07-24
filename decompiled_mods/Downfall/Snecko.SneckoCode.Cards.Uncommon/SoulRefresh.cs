using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Cards.Token;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class SoulRefresh : SneckoCardModel
{
	public SoulRefresh()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithTip<SoulRoll>();
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await CardCmd.TransformTo<SoulRoll>(val, (CardPreviewStyle)1);
		}
	}
}
