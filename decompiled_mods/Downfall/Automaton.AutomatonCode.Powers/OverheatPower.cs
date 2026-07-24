using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class OverheatPower : AutomatonPowerModel
{
	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? creator)
	{
		if (creator != null && creator.Creature == ((PowerModel)this).Applier)
		{
			((PowerModel)this).Flash();
			await DownfallCreatureCmd.Damage(ctx, ((PowerModel)this).Owner, ((PowerModel)this).Amount, (ValueProp)6, card.Owner.Creature, card, null);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public OverheatPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
