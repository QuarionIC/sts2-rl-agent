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
public class Artifice : AwakenedCardModel
{
	protected override Artist? Artist => Downfall.DownfallCode.Artists.Artist.Get<Chimedragon>();

	public Artifice()
		: base(2, (CardType)2, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<ManaburnPower>(7, 3);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<ManaburnPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
