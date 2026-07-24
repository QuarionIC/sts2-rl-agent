using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class PowerFromBeyond : HexaghostCardModel, IHasAfterlifeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public PowerFromBeyond()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithPower<VigorPower>(3, 1);
		((ConstructedCardModel)this).WithEnergy(2, 1);
		((ConstructedCardModel)(object)this).WithPower<EnergyNextTurnPower>(2, 1, showTooltip: false);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<VigorPower>(ctx, (CardModel)(object)this, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await AfterlifeEffect(ctx, cardPlay);
		await CommonActions.ApplySelf<EnergyNextTurnPower>(ctx, (CardModel)(object)this, false);
	}
}
