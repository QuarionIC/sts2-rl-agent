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
public class OlexasShield : HexaghostRelicModel, IGhostflameConditionOverwrites
{
	public OlexasShield()
		: base((RelicRarity)3)
	{
	}

	public bool GhostflameConditionOverwrites(Player player, GhostflameModel ghostflame, CardPlay cardPlay)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		bool flag = player == ((RelicModel)this).Owner;
		if (flag)
		{
			bool flag2 = ((ghostflame is SearingGhostflame || ghostflame is CrushingGhostflame) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return (int)cardPlay.Card.Type == 3;
		}
		return false;
	}
}
