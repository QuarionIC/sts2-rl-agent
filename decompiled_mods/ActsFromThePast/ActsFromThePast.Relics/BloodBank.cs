using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class BloodBank : CustomRelicModel
{
	private const int GoldPerExcessHp = 10;

	private int _hpBeforeRest;

	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("GoldPerHp", 10m) };

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is RestSiteRoom)
		{
			_hpBeforeRest = ((RelicModel)this).Owner.Creature.CurrentHp;
		}
	}

	public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
	{
		if (player == ((RelicModel)this).Owner)
		{
			int actualHealed = ((RelicModel)this).Owner.Creature.CurrentHp - _hpBeforeRest;
			int intendedHeal = (int)HealRestSiteOption.GetHealAmount(player);
			int excess = intendedHeal - actualHealed;
			if (excess > 0)
			{
				((RelicModel)this).Flash();
				await PlayerCmd.GainGold((decimal)(excess * 10), ((RelicModel)this).Owner, false);
			}
		}
	}

	public BloodBank()
		: base(true)
	{
	}
}
