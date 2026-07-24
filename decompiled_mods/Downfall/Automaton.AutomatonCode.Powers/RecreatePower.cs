using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Powers;

public class RecreatePower : AutomatonPowerModel
{
	public RecreatePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<Fuel>();
	}

	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator != null && creator.Creature == ((PowerModel)this).Owner && (int)card.Type == 4 && CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count((CardGeneratedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && e.Creator == creator && (int)e.Card.Type == 4) <= ((PowerModel)this).Amount)
		{
			((PowerModel)this).Flash();
			await CardCmd.TransformTo<Fuel>(card, (CardPreviewStyle)1);
		}
	}
}
