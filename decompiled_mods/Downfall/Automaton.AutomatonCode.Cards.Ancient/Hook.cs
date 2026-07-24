using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Ancient;

[Pool(typeof(AutomatonCardPool))]
public class Hook : AutomatonCardModel
{
	public Hook()
		: base(0, (CardType)2, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(10, 4);
		WithStash(1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await StashCmd.StashFromDraw((CardModel)(object)this, ctx);
	}
}
