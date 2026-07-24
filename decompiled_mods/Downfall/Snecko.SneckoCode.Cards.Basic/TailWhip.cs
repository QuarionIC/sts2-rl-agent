using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Cards.Ancient;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Basic;

[Pool(typeof(SneckoCardPool))]
public class TailWhip : SneckoCardModel, IHasOverflowEffect, ITranscendenceCard
{
	public TailWhip()
		: base(2, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithDamage(10, 2);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 1);
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<VulnerablePower>(ctx, (CardModel)(object)this, cardPlay, false);
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<Whiplash>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
