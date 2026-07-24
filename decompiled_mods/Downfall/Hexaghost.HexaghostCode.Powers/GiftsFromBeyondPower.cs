using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class GiftsFromBeyondPower : HexaghostPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			IEnumerable<CardModel> enumerable = from c in player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
				where c.Keywords.Contains(HexaghostKeyword.Afterlife)
				select c;
			await CardPileCmd.AddGeneratedCardsToCombat(CardFactory.GetDistinctForCombat(player, enumerable, ((PowerModel)this).Amount, player.RunState.Rng.CombatCardGeneration), (PileType)2, player, (CardPilePosition)1);
		}
	}

	public GiftsFromBeyondPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
