using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Events;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class DefensiveThesis : ChampRelicModel, IModifyDefensiveFinisherBonus
{
	public DefensiveThesis()
		: base((RelicRarity)3)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		WithTips((RelicModel _) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(ChampModelDb.ChampStance<ChampDefensiveStance>().HoverTip));
		WithTip(ChampTip.Finisher);
	}

	public int ModifyDefensiveFinisherBonus(ChampStanceModel stanceModel, int baseAmount)
	{
		if (stanceModel.Owner != ((RelicModel)this).Owner)
		{
			return baseAmount;
		}
		return baseAmount + 3;
	}
}
