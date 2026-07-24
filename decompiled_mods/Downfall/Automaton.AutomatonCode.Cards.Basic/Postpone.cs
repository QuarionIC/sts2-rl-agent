using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Ancient;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Basic;

[Pool(typeof(AutomatonCardPool))]
public class Postpone : AutomatonCardModel, ITranscendenceCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Postpone()
		: base(2, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(10, 4);
		WithStash(1);
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<Hook>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await StashCmd.StashFromHand((CardModel)(object)this, ctx);
	}
}
