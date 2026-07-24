using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class Floatwork : HexaghostCardModel, IHasAfterlifeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public Floatwork()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithPower<DexterityPower>(1, 1);
		((ConstructedCardModel)this).WithPower<MetallicizePower>(2, 0);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<MetallicizePower>(ctx, (CardModel)(object)this, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
		await AfterlifeEffect(ctx, cardPlay);
	}
}
