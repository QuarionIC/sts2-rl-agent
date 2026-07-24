using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Piles;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class Mallet : AutomatonRelicModel
{
	public Mallet()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AutomatonTip.Stash);
	}

	protected override Task AfterCardChangedPiles(PlayerChoiceContext ctx, CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (card.Owner == ((RelicModel)this).Owner)
		{
			CardPile pile = card.Pile;
			if (((pile != null) ? new PileType?(pile.Type) : ((PileType?)null)) == (PileType?)StashPile.Stash)
			{
				CardCmd.Upgrade(card, (CardPreviewStyle)1);
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}
}
