using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class ShieldOfNight : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Zhen>();

	public ShieldOfNight()
		: base(2, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(12, 3);
		((ConstructedCardModel)(object)this).WithScry(3, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)2));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		ScryResult val = await ScryCmd.Execute(ctx, (CardModel)(object)this);
		foreach (CardModel item in ((ScryResult)(ref val)).Discarded.Where((CardModel card) => card.Keywords.Contains((CardKeyword)2)))
		{
			await CardCmd.Exhaust(ctx, item, false, false);
		}
	}
}
