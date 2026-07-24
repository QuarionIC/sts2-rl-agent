using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class EnsorcelatePower : AwakenedPowerModel
{
	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!IsEligiblePower(card))
		{
			return false;
		}
		modifiedCost = default(decimal);
		return true;
	}

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (IsEligiblePower(cardPlay.Card))
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	private bool IsEligiblePower(CardModel card)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		bool flag = (int)card.Type == 3 && card.Owner.Creature == ((PowerModel)this).Owner;
		bool flag2;
		if (flag)
		{
			CardPile pile = card.Pile;
			PileType? val = ((pile != null) ? new PileType?(pile.Type) : ((PileType?)null));
			if (val.HasValue)
			{
				PileType valueOrDefault = val.GetValueOrDefault();
				if ((int)valueOrDefault == 2 || (int)valueOrDefault == 5)
				{
					flag2 = true;
					goto IL_0063;
				}
			}
			flag2 = false;
			goto IL_0063;
		}
		goto IL_0065;
		IL_0065:
		return flag;
		IL_0063:
		flag = flag2;
		goto IL_0065;
	}

	public EnsorcelatePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
