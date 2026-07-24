using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Intensify : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Intensify()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<IntensifyPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<BurnoutPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithConjure();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
		await CommonActions.ApplySelf<IntensifyPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<BurnoutPower>(ctx, (CardModel)(object)this, false);
	}
}
