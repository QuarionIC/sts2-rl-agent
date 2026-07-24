using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class TakeFlight : AwakenedCardModel, IChantable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Eudaimonia>();

	public bool HasChanted { get; set; }

	public TakeFlight()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(12, 3);
		((ConstructedCardModel)(object)this).WithPower<BlurPower>(1, showTooltip: false);
	}

	public async Task PlayChantEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<BlurPower>(ctx, (CardModel)(object)this, 1m, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
