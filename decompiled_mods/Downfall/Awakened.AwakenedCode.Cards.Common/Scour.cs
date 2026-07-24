using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Scour : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Scour()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithPower<ManaburnPower>(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
			await CommonActions.Apply<ManaburnPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
		}
	}
}
