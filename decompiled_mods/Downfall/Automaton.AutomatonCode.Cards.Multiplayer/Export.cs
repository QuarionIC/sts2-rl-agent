using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Multiplayer;

[Pool(typeof(AutomatonCardPool))]
public class Export : AutomatonCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Export()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)6)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithBlock(5, 0);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		Creature target = cardPlay.Target;
		if (((target != null) ? target.Player : null) != null)
		{
			await PlayerCmd.GainEnergy((decimal)((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, cardPlay.Target.Player);
		}
	}
}
