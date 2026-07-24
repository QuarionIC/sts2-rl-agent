using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Basic;

[Pool(typeof(AwakenedCardPool))]
public class Hymn : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Hymn()
		: base(0, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(3, 3);
		((ConstructedCardModel)(object)this).WithTip<Ceremony>();
		((ConstructedCardModel)(object)this).WithDrained(1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			await CommonActions.CardBlock((CardModel)(object)this, ((CardModel)this).DynamicVars.Block, cardPlay);
			await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)((CardModel)this).CombatState.CreateCard<Ceremony>(((CardModel)this).Owner), (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
			await CommonActions.ApplySelf<DrainedPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
