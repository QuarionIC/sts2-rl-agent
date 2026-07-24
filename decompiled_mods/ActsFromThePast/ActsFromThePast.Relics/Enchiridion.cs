using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class Enchiridion : CustomRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((RelicModel)this).Owner && ((RelicModel)this).Owner.Creature.CombatState.RoundNumber == 1)
		{
			((RelicModel)this).Flash();
			List<CardModel> powerCards = (from c in ((RelicModel)this).Owner.Character.CardPool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where (int)c.Type == 3
				select c).ToList();
			CardModel card = CardFactory.GetDistinctForCombat(((RelicModel)this).Owner, (IEnumerable<CardModel>)powerCards, 1, ((RelicModel)this).Owner.RunState.Rng.CombatCardGeneration).First();
			card.SetToFreeThisTurn();
			await CardPileCmd.AddGeneratedCardToCombat(card, (PileType)2, ((RelicModel)this).Owner, (CardPilePosition)1);
		}
	}

	public Enchiridion()
		: base(true)
	{
	}
}
