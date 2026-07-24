using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class Blunderbus : SneckoCardModel
{
	private int ThreeCostInHand => ((CardModel)this).Owner.GetHand().Count((CardModel e) => (decimal)e.EnergyCost.GetResolved() >= ((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue);

	public Blunderbus()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithEnergy(3, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1 + ThreeCostInHand, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
