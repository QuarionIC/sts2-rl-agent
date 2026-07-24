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

namespace Hexaghost.HexaghostCode.Cards.Ancient;

[Pool(typeof(HexaghostCardPool))]
public class InfernalForm : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public InfernalForm()
		: base(3, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<InfernalFormPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithTip<IntensityPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<InfernalFormPower>(ctx, (CardModel)(object)this, false);
	}
}
