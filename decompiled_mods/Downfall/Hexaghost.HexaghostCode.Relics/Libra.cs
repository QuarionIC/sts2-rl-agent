using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using Hexaghost.HexaghostCode.Ghostflames;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class Libra : HexaghostRelicModel, IGhostflameConditionOverwrites
{
	public Libra()
		: base((RelicRarity)5)
	{
	}

	public bool GhostflameConditionOverwrites(Player player, GhostflameModel ghostflame, CardPlay cardPlay)
	{
		bool flag = player == ((RelicModel)this).Owner;
		if (flag)
		{
			bool flag2 = ((ghostflame is SearingGhostflame || ghostflame is CrushingGhostflame) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return cardPlay.Card.IsBasicStrikeOrDefend;
		}
		return false;
	}
}
