using System;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Relics;

[Obsolete]
public sealed class DentedPlate : HermitRelicModel
{
	public DentedPlate()
		: base((RelicRarity)4, autoAdd: false)
	{
		WithEnergy(1);
		WithCards(1);
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != ((RelicModel)this).Owner || player.Creature.CurrentHp > player.Creature.MaxHp / 2)
		{
			return count;
		}
		return count + ((DynamicVar)((RelicModel)this).DynamicVars.Cards).BaseValue;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != ((RelicModel)this).Owner || player.Creature.CurrentHp > player.Creature.MaxHp / 2)
		{
			return amount;
		}
		return amount + ((DynamicVar)((RelicModel)this).DynamicVars.Energy).BaseValue;
	}
}
