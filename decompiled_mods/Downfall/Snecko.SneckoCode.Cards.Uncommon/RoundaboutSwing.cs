using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class RoundaboutSwing : SneckoCardModel
{
	public RoundaboutSwing()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithPower<DrawCardsNextTurnPower>(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel val = (await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.ToTopSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)1, (CardPilePosition)2, (AbstractModel)null, false);
		}
		await CommonActions.ApplySelf<DrawCardsNextTurnPower>(ctx, (CardModel)(object)this, false);
	}
}
