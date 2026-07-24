using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class CobraCoil : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public CobraCoil()
		: base(4, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)4,
			Type = (CardType)1
		});
		((ConstructedCardModel)this).WithDamage(20, 4);
		((ConstructedCardModel)(object)this).WithPower<SneckoConstrictPower>(10, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.Apply<SneckoConstrictPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
