using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class SummonOrbPower : AutomatonPowerModel
{
	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		bool flag = cardPlay.Card.Owner.Creature != ((PowerModel)this).Owner || !cardPlay.IsFirstInSeries || cardPlay.Card.Keywords.Contains((CardKeyword)1) || AutomatonCmd.IsEncodable(cardPlay.Card);
		if (!flag)
		{
			CardType type = cardPlay.Card.Type;
			bool flag2 = type - 1 <= 1;
			flag = !flag2;
		}
		if (!flag && CombatManager.Instance.History.CardPlaysStarted.Count(delegate(CardPlayStartedEntry e)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Invalid comparison between Unknown and I4
			bool flag3 = ((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner;
			if (flag3)
			{
				CardType type2 = e.CardPlay.Card.Type;
				bool flag4 = type2 - 1 <= 1;
				flag3 = flag4;
			}
			return flag3 && e.CardPlay.IsFirstInSeries && ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState);
		}) <= ((PowerModel)this).Amount)
		{
			await StashCmd.Stash(cardPlay.Card);
			((PowerModel)this).Flash();
		}
	}

	public SummonOrbPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
