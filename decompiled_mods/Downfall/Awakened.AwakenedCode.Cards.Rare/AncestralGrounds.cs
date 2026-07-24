using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class AncestralGrounds : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public AncestralGrounds()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(12, 0);
		((ConstructedCardModel)this).WithEnergy(2, 1);
		((ConstructedCardModel)(object)this).WithPower<AncestralGroundsPower>(2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<AncestralGroundsUpgradedPower>(2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<Void>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (((CardModel)this).IsUpgraded)
		{
			await CommonActions.ApplySelf<AncestralGroundsUpgradedPower>(ctx, (CardModel)(object)this, false);
		}
		else
		{
			await CommonActions.ApplySelf<AncestralGroundsPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
