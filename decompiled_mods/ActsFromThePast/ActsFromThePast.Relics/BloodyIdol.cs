using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class BloodyIdol : CustomRelicModel
{
	private const int HealAmount = 5;

	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new HealVar(5m) };

	public override async Task AfterGoldGained(Player player)
	{
		if (player == ((RelicModel)this).Owner)
		{
			((RelicModel)this).Flash();
			await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue, true);
		}
	}

	public BloodyIdol()
		: base(true)
	{
	}
}
