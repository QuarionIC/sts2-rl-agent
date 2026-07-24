using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class FaceOfCleric : CustomRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new MaxHpVar(1m) };

	public override async Task AfterCombatEnd(CombatRoom _)
	{
		((RelicModel)this).Flash();
		await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.MaxHp).BaseValue);
	}

	public FaceOfCleric()
		: base(true)
	{
	}
}
