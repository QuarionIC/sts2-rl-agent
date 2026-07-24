using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class WasteNot : SlimeBossCardModel
{
	public WasteNot()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithSlurp(1m);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await SlimeBossCmd.SlurpAll((CardModel)(object)this);
	}
}
