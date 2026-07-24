using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class ChosenVerse : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ChosenVerse()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<ChosenVersePower>(4, 2, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		ChosenVersePower chosenVersePower = await CommonActions.ApplySelf<ChosenVersePower>(ctx, (CardModel)(object)this, 2m, false);
		if (chosenVersePower != null)
		{
			chosenVersePower.SetBlock(DynamicVarSetExtensions.Power<ChosenVersePower>(((CardModel)this).DynamicVars).IntValue);
			chosenVersePower.CardPlay = cardPlay;
		}
	}
}
