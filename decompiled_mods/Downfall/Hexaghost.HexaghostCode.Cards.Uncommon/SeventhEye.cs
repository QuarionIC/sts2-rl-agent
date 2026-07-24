using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class SeventhEye : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Claude27A>();

	public SeventhEye()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = (await DownfallCardCmd.SelectFromCards(ctx, ((CardModel)this).Owner.GetDraw(), DownfallCardSelectorPrefs.ToHandSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
		await HexaghostCmd.MoveToRandom(ctx, ((CardModel)this).Owner, silent: true);
		await HexaghostCmd.ReplaceCurrentWithRandom(((CardModel)this).Owner);
	}
}
