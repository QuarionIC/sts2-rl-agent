using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class MulliganPower : SneckoPowerModel
{
	private static bool CostMoreThanNormal(CardPlay? play)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (((play != null) ? play.Card : null) == null)
		{
			return false;
		}
		if (play.Card.EnergyCost.CostsX)
		{
			return false;
		}
		ResourceInfo resources = play.Resources;
		return ((ResourceInfo)(ref resources)).EnergyValue > play.Card.EnergyCost.GetWithModifiers((CostModifiers)0);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player owner = cardPlay.Card.Owner;
		if (owner.Creature == ((PowerModel)this).Owner && CostMoreThanNormal(cardPlay) && CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && e.CardPlay != cardPlay && ((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && CostMoreThanNormal(e.CardPlay)) < ((PowerModel)this).Amount)
		{
			((PowerModel)this).Flash();
			await PlayerCmd.GainEnergy(1m, owner);
		}
	}

	public MulliganPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
