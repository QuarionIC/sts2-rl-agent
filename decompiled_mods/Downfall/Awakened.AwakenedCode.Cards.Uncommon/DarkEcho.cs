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
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class DarkEcho : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Eudaimonia>();

	public DarkEcho()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithPower<DarkEchoPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DarkEchoPower>(ctx, (CardModel)(object)this, false);
	}
}
