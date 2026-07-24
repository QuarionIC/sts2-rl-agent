using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Goto : AutomatonCardModel
{
	public Goto()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 1);
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithEnergy(0, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		if (creator != ((CardModel)this).Owner || card.Owner != ((CardModel)this).Owner || (int)card.Type != 4)
		{
			return Task.CompletedTask;
		}
		((CardModel)this).EnergyCost.SetUntilPlayed(((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, false);
		return Task.CompletedTask;
	}
}
