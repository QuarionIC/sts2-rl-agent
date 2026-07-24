using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class BurningTouch : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public BurningTouch()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(8, 4);
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			bool cond = cardPlay.Target.HasPower<SoulBurnPower>();
			await CommonActions.Apply<SoulBurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
			if (cond)
			{
				await CommonActions.Draw((CardModel)(object)this, ctx);
			}
		}
	}
}
