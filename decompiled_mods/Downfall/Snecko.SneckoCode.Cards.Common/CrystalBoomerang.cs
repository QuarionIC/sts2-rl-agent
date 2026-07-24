using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class CrystalBoomerang : SneckoCardModel
{
	public CrystalBoomerang()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(5, 3);
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel card = (await DownfallCardCmd.SelectFromCards(ctx, ((CardModel)this).Owner.GetDiscard(), DownfallCardSelectorPrefs.ToHandSelectionPrompt, (CardModel)(object)this, optional: true)).FirstOrDefault();
		if (card != null)
		{
			await CardPileCmd.Add(card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			if (SneckoCmd.IsOffclass(card))
			{
				await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
			}
		}
	}
}
