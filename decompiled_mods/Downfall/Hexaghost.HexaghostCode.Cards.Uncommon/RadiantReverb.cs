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

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class RadiantReverb : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public RadiantReverb()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(14, 4);
		((ConstructedCardModel)(object)this).WithPower<RadiantReverbPower>(3, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<IntensityPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.ApplySelf<RadiantReverbPower>(ctx, (CardModel)(object)this, false);
	}
}
