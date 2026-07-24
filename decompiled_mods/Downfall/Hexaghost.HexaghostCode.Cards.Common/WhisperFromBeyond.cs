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

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class WhisperFromBeyond : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public WhisperFromBeyond()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(12, 3);
		((ConstructedCardModel)(object)this).WithPower<WhisperFromBeyondPower>(5, 2, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)(object)this).WithTip<SoulBurnPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.Apply<WhisperFromBeyondPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
