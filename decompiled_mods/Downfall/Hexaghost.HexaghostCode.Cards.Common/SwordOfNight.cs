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
public class SwordOfNight : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Zhen>();

	public SwordOfNight()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(14, 4);
		((ConstructedCardModel)(object)this).WithScry(3, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)2));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		ScryResult val = await ScryCmd.Execute(ctx, (CardModel)(object)this);
		foreach (CardModel item in ((ScryResult)(ref val)).Discarded.Where((CardModel card) => card.Keywords.Contains((CardKeyword)2)))
		{
			await CardCmd.Exhaust(ctx, item, false, false);
		}
	}
}
