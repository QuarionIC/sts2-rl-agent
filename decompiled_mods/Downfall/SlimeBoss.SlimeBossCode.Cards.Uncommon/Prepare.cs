using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class Prepare : SlimeBossCardModel
{
	public Prepare()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(10, 5);
		((ConstructedCardModel)this).WithEnergy(1, 0);
		((ConstructedCardModel)(object)this).WithPower<EnergyNextTurnPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<DrawCardsNextTurnPower>(2, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<EnergyNextTurnPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<DrawCardsNextTurnPower>(ctx, (CardModel)(object)this, false);
	}
}
