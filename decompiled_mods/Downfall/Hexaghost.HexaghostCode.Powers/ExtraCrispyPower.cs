using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Events;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Powers;

public class ExtraCrispyPower : HexaghostPowerModel, IAfterSoulburnDetonate
{
	public async Task AfterSoulburnDetonate(PlayerChoiceContext ctx, Creature creature)
	{
		if (((PowerModel)this).Owner.CombatState != null && ((PowerModel)this).Owner.CombatState.Enemies.Contains(creature))
		{
			await CreatureCmd.Damage(ctx, creature, (decimal)((PowerModel)this).Amount, (ValueProp)14, ((PowerModel)this).Owner);
			await PowerCmd.Apply<SoulBurnPower>(ctx, creature, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public ExtraCrispyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
