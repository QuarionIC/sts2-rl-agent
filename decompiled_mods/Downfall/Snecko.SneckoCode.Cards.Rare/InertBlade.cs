using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class InertBlade : SneckoCardModel
{
	public InertBlade()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(10, 3);
		((ConstructedCardModel)this).WithCards(3, 1);
		((ConstructedCardModel)this).WithBlock(9, 3);
		((ConstructedCardModel)this).WithPower<StrengthPower>(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int cost = ((CardModel)this).EnergyCost.GetResolved();
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (cost < 1)
		{
			return;
		}
		await CommonActions.Draw((CardModel)(object)this, ctx);
		if (cost >= 2)
		{
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
			if (cost >= 3)
			{
				await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
			}
		}
	}
}
