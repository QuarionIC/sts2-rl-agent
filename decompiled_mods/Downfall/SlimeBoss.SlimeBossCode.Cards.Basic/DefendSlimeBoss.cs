using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Basic;

[Pool(typeof(SlimeBossCardPool))]
public class DefendSlimeBoss : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<HalfGoblinHankins>();

	public DefendSlimeBoss()
		: base(1, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)2 });
		((ConstructedCardModel)this).WithBlock(5, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
