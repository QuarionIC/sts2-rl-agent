using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Rare;

public class Burdened : HermitCardModel
{
	public Burdened()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<BurdenedPower>(8, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<VigorPower>();
		((ConstructedCardModel)(object)this).WithTip<Decay>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		(await MyCommonActions.ApplySelf<BurdenedPower>(ctx, (AbstractModel)(object)this))?.IncrementSelfDamage();
	}
}
