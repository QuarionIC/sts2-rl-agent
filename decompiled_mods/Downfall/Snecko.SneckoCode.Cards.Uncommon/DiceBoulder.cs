using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class DiceBoulder : SneckoCardModel
{
	public DiceBoulder()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 1);
		((ConstructedCardModel)this).WithVar("Increase", 4, 1);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		((DynamicVar)((CardModel)this).DynamicVars.Block).UpgradeValueBy(((CardModel)this).DynamicVars["Increase"].BaseValue);
		((CardModel)this).EnergyCost.AddThisCombat(((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, false);
	}
}
