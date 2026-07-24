using System;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Hermit.HermitCode.Relics;

[Obsolete]
public sealed class BrokenTooth : HermitRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)4;

	public BrokenTooth()
		: base((RelicRarity)4, autoAdd: false)
	{
		WithHeal(7);
		WithGold(35);
	}

	public override async Task AfterCombatVictory(CombatRoom room)
	{
		if ((int)((AbstractRoom)room).RoomType == 2)
		{
			((RelicModel)this).Flash();
			await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue, true);
			await PlayerCmd.GainGold(((DynamicVar)((RelicModel)this).DynamicVars.Gold).BaseValue, ((RelicModel)this).Owner, false);
		}
	}
}
