using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Powers;

public class MergeConflictPower : AutomatonPowerModel
{
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? player)
	{
		if (((player != null) ? player.Creature : null) != ((PowerModel)this).Owner || !(card is FunctionCard))
		{
			return;
		}
		CardPile pile = card.Pile;
		PileType? pile2 = ((pile != null) ? new PileType?(pile.Type) : ((PileType?)null));
		if (pile2.HasValue)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
			((PowerModel)this).Flash();
			CardPileAddResult val = await CardPileCmd.AddGeneratedCardToCombat(card.CreateClone(), pile2.Value, player, (CardPilePosition)1);
			if ((int)pile2.GetValueOrDefault() != 2)
			{
				CardCmd.PreviewCardPileAdd(val, 1.2f, (CardPreviewStyle)1);
			}
		}
	}

	public MergeConflictPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
