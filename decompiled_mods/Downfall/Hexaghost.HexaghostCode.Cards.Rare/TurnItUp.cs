using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class TurnItUp : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public TurnItUp()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<TurnItUpStrengthPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<TurnItUpDexterityPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<TurnItUpIntensityPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithTip<DexterityPower>();
		((ConstructedCardModel)(object)this).WithTip<IntensityPower>();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<TurnItUpStrengthPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<TurnItUpDexterityPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<TurnItUpIntensityPower>(ctx, (CardModel)(object)this, false);
	}
}
