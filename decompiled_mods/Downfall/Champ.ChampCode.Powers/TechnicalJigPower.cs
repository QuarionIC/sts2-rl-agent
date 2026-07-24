using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class TechnicalJigPower : ChampPowerModel, IOnChampStanceChange
{
	public async Task OnChampStanceChange(PlayerChoiceContext ctx, Player player, ChampStanceModel oldStance, ChampStanceModel newStance)
	{
		if (player.Creature == ((PowerModel)this).Owner && !(newStance is ChampNoStance))
		{
			await CreatureCmd.GainBlock(player.Creature, (decimal)((PowerModel)this).Amount, (ValueProp)12, (CardPlay)null, false);
		}
	}

	public TechnicalJigPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
