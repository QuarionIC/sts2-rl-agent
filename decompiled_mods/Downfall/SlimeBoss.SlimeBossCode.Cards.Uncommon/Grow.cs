using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class Grow : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public Grow()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(2, 0);
		((ConstructedCardModel)this).WithPower<DexterityPower>(2, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (await SlimeBossCmd.DecreaseSlots(ctx, ((CardModel)this).Owner) > 0)
		{
			await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
			await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
