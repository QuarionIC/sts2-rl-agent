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
public class Sizzle : HexaghostCardModel
{
	protected override Artist? Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public Sizzle()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<SizzlePower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithBlock(11, 4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<SizzlePower>(ctx, (CardModel)(object)this, false);
	}
}
