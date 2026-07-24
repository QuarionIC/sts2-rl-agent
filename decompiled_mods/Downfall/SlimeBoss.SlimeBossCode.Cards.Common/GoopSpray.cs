using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class GoopSpray : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public GoopSpray()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)3)
	{
		((ConstructedCardModel)this).WithPower<GoopPower>(5, 3);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
