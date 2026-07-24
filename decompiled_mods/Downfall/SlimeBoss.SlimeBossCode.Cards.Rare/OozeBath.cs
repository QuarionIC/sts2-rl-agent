using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class OozeBath : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public OozeBath()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithPower<OozeBathPower>(6, 3, showTooltip: false);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<OozeBathPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
