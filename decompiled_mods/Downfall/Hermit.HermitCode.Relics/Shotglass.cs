using System.Collections.Generic;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Hermit.HermitCode.Relics;

public sealed class Shotglass : HermitRelicModel
{
	public int AvailableUses { get; set; }

	public bool IsInCombat { get; set; }

	public override bool ShowCounter => IsInCombat;

	public override int DisplayAmount => AvailableUses;

	public Shotglass()
		: base((RelicRarity)5)
	{
		WithVar("Limit", 1);
	}

	public override Task BeforeCombatStart()
	{
		AvailableUses = (int)((RelicModel)this).DynamicVars["Limit"].BaseValue;
		IsInCombat = true;
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		if (potion.Owner == ((RelicModel)this).Owner && AvailableUses != 0 && IsInCombat)
		{
			AvailableUses--;
			((RelicModel)this).Flash();
			await PotionCmd.TryToProcure(PotionFactory.CreateRandomPotionInCombat(((RelicModel)this).Owner, ((RelicModel)this).Owner.RunState.Rng.CombatPotionGeneration, (IEnumerable<PotionModel>)null).ToMutable(), ((RelicModel)this).Owner, -1);
			((RelicModel)this).InvokeDisplayAmountChanged();
			if (AvailableUses == 0)
			{
				((RelicModel)this).Status = (RelicStatus)2;
			}
		}
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		IsInCombat = false;
		((RelicModel)this).Status = (RelicStatus)0;
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}
}
