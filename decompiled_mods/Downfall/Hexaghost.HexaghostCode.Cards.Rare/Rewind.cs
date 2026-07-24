using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class Rewind : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public Rewind()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithKeyword(HexaghostKeyword.Retract, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CardPileCmd.Add(await DownfallCardCmd.SelectFromCards(ctx, ((CardModel)this).Owner.GetDiscard(), DownfallCardSelectorPrefs.ToHandSelectionPrompt, (CardModel)(object)this, optional: true), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
