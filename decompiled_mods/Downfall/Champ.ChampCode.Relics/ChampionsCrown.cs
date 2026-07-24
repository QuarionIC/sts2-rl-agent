using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class ChampionsCrown : ChampRelicModel
{
	public ChampionsCrown()
		: base((RelicRarity)1)
	{
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<VictoriousCrown>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				((RelicModel)this).Flash();
				await ChampCmd.EnterDifferentStance(ctx, player);
				ChampStanceModel stance = ((RelicModel)this).Owner.ChampStance();
				await stance.SkillBonus(ctx);
				await stance.SkillBonus(ctx);
			}
		}
	}
}
