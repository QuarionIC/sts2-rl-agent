using System.Threading.Tasks;
using Downfall.DownfallCode.Events;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class LivingBombPower : HexaghostPowerModel, IShouldSoulburnDetonateTargetAll, IAfterSoulburnDetonate
{
	public LivingBombPower()
		: base((PowerType)2, (PowerStackType)2)
	{
	}

	public async Task AfterSoulburnDetonate(PlayerChoiceContext ctx, Creature creature)
	{
		if (creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public bool ShouldSoulburnDetonateTargetAll(PlayerChoiceContext ctx, Creature owner)
	{
		return owner == ((PowerModel)this).Owner;
	}
}
