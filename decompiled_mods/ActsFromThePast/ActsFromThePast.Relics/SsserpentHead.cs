using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class SsserpentHead : CustomRelicModel
{
	private const int Gold = 50;

	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new GoldVar(50) };

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (!((RelicModel)this).Owner.Creature.IsDead)
		{
			MapPoint currentMapPoint = ((RelicModel)this).Owner.RunState.CurrentMapPoint;
			if (currentMapPoint != null && (int)currentMapPoint.PointType == 1)
			{
				((RelicModel)this).Flash();
				await PlayerCmd.GainGold(((DynamicVar)((RelicModel)this).DynamicVars.Gold).BaseValue, ((RelicModel)this).Owner, false);
			}
		}
	}

	public SsserpentHead()
		: base(true)
	{
	}
}
