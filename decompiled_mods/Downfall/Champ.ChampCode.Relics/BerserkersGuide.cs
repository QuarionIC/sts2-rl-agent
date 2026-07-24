using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class BerserkersGuide : ChampRelicModel
{
	public BerserkersGuide()
		: base((RelicRarity)2)
	{
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			((RelicModel)this).Flash();
			await PowerCmd.Apply<VigorPower>(ctx, player.Creature, 3m, player.Creature, (CardModel)null, true);
		}
	}
}
