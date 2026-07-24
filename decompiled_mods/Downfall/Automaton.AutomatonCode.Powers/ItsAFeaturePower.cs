using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Powers;

public class ItsAFeaturePower : AutomatonPowerModel
{
	public ItsAFeaturePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<VigorPower>();
	}

	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? creator)
	{
		if (creator != null && creator.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Apply<VigorPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
