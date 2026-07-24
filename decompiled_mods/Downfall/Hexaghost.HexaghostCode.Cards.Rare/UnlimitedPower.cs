using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class UnlimitedPower : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public UnlimitedPower()
		: base(4, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await HexaghostCmd.IgniteAll(ctx, ((CardModel)this).Owner);
	}
}
