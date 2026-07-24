using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
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

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class BloodiedPreening : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public BloodiedPreening()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithTip<PlumeJab>();
		((ConstructedCardModel)this).WithVar("StrengthLoss", 2, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, -((CardModel)this).DynamicVars["StrengthLoss"].BaseValue, false);
		await CommonActions.ApplySelf<BloodiedPreeningPower>(ctx, (CardModel)(object)this, 1m, false);
	}
}
