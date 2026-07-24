using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class CatchUp : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public CatchUp()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithVar("IgniteCount", 2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		for (int i = 0; i < ((CardModel)this).DynamicVars["IgniteCount"].IntValue; i++)
		{
			await HexaghostCmd.IgnitePrevious(ctx, ((CardModel)this).Owner);
		}
	}
}
