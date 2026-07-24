using System.Collections.Generic;
using System.Linq;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public class DeathwishPower : HermitPowerModel, IShouldTriggerDeadOn
{
	public DeathwishPower()
		: base((PowerType)1, (PowerStackType)2)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(HermitKeywords.DeadOn);
	}

	public bool ShouldTriggerDeadOn(CardModel card)
	{
		if (card.Owner.Creature != ((PowerModel)this).Owner)
		{
			return false;
		}
		List<CardModel> list = PileTypeExtensions.GetPile((PileType)2, card.Owner).Cards.ToList();
		int num = list.IndexOf(card);
		if (num == -1)
		{
			return false;
		}
		bool num2 = num > 0 && IsCurse(list[num - 1]);
		bool flag = num < list.Count - 1 && IsCurse(list[num + 1]);
		return num2 || flag;
	}

	private static bool IsCurse(CardModel c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)c.Type == 5;
	}
}
