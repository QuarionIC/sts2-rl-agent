using System;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Relics;

[Obsolete]
public sealed class PetGhost : HermitRelicModel
{
	private bool _usedThisCombat;

	private bool UsedThisCombat
	{
		get
		{
			return _usedThisCombat;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedThisCombat = value;
		}
	}

	public PetGhost()
		: base((RelicRarity)4, autoAdd: false)
	{
	}

	public override Task BeforeCombatStart()
	{
		((RelicModel)this).Status = (RelicStatus)0;
		UsedThisCombat = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != ((RelicModel)this).Owner.Creature || amount < (decimal)((RelicModel)this).Owner.Creature.CurrentHp || UsedThisCombat)
		{
			return amount;
		}
		((RelicModel)this).Flash();
		((RelicModel)this).Status = (RelicStatus)2;
		UsedThisCombat = true;
		return 0m;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		((RelicModel)this).Status = (RelicStatus)0;
		return Task.CompletedTask;
	}
}
