using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class ImprovisingPower : ChampPowerModel, IOnChampStanceChange
{
	public async Task OnChampStanceChange(PlayerChoiceContext ctx, Player player, ChampStanceModel oldStance, ChampStanceModel newStance)
	{
		if (player.Creature == ((PowerModel)this).Owner && !(newStance is ChampNoStance))
		{
			for (int i = 0; i < ((PowerModel)this).Amount; i++)
			{
				await newStance.SkillBonus(ctx);
			}
		}
	}

	public ImprovisingPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
