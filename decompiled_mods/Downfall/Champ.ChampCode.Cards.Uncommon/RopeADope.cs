using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class RopeADope : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public RopeADope()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithFinisher();
		((ConstructedCardModel)this).WithBlock(8, 2);
		((ConstructedCardModel)this).WithEnergy(1, 1);
		((ConstructedCardModel)(object)this).WithPower<DrawCardsNextTurnPower>(2, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<EnergyNextTurnPower>(ctx, (CardModel)(object)this, ((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue, false);
		await CommonActions.ApplySelf<DrawCardsNextTurnPower>(ctx, (CardModel)(object)this, false);
	}
}
