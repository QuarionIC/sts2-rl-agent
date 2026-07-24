using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Cards.Multiplayer;

[Pool(typeof(AutomatonCardPool))]
public class Uptick : AutomatonCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Uptick()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)7)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithPower<DrawCardsNextTurnPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<EnergyNextTurnPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<DrawCardsNextTurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<EnergyNextTurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
