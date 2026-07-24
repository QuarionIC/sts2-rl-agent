using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class LivingBomb : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	public LivingBomb()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(5, 3);
		((ConstructedCardModel)(object)this).WithPower<LivingBombPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<SoulBurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<LivingBombPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
