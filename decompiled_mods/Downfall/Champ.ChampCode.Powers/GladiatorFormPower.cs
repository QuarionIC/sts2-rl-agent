using System.Linq;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class GladiatorFormPower : ChampPowerModel, IModifyDamageMultiplicative
{
	public decimal ModifyDamageMultiplicativeCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		if (!ValuePropExtensions.IsPoweredAttack(props) || cardSource == null || cardSource.Owner.Creature != ((PowerModel)this).Owner)
		{
			return 1m;
		}
		int num = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && (int)e.CardPlay.Card.Type == 1 && e.CardPlay.Card.Owner.Creature == ((PowerModel)this).Owner);
		CardPile pile = cardSource.Pile;
		int num2 = ((pile != null && (int)pile.Type == 5) ? 1 : 0);
		if (num - num2 >= ((PowerModel)this).Amount)
		{
			return 1m;
		}
		return 2m;
	}

	public GladiatorFormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
