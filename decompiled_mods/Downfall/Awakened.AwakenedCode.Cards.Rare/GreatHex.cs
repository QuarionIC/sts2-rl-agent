using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class GreatHex : AwakenedCardModel, IChantable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public bool HasChanted { get; set; }

	public GreatHex()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithPower<GreatHexPower>(5, 3, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<ManaburnPower>();
	}

	public async Task PlayChantEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<GreatHexPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
