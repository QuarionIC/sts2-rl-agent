using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class MarkOfTheEther : HexaghostRelicModel, IAfterGhostflameIgnited
{
	public MarkOfTheEther()
		: base((RelicRarity)1)
	{
	}

	public async Task AfterGhostflameIgnited(PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		if (player == ((RelicModel)this).Owner)
		{
			((RelicModel)this).Flash();
			await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, 4m, (ValueProp)12, (CardPlay)null, true);
		}
	}
}
