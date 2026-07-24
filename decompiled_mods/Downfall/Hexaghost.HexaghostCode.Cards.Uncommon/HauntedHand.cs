using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class HauntedHand : HexaghostCardModel, IHasAfterlifeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public HauntedHand()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithBlock(5, 3);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		while (CardPile.GetCards(((CardModel)this).Owner, (PileType[])(object)new PileType[1] { (PileType)2 }).Count() < 10)
		{
			CardModel val = await CardPileCmd.Draw(ctx, ((CardModel)this).Owner);
			if (val == null || !val.Keywords.Contains((CardKeyword)2))
			{
				break;
			}
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await AfterlifeEffect(ctx, cardPlay);
	}
}
