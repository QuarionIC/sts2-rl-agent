using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class TimeOfNeed : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public TimeOfNeed()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, from c in ((CardModel)this).Owner.Character.CardPool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where (int)c.Type == 3
			select c, 1, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
		if (val != null)
		{
			val.SetToFreeThisTurn();
			if (((CardModel)this).IsUpgraded)
			{
				val.UpgradeInternal();
			}
			await CardPileCmd.AddGeneratedCardToCombat(val, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
		}
	}
}
