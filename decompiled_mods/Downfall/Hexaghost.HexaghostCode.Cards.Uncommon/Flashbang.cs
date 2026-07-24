using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class Flashbang : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public Flashbang()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 1);
		((ConstructedCardModel)(object)this).WithPower<FlashbangPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (HexaghostCmd.IsIgnited(((CardModel)this).Owner))
		{
			await CommonActions.Apply<FlashbangPower>(ctx, (CardModel)(object)this, cardPlay, false);
			await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		}
	}
}
