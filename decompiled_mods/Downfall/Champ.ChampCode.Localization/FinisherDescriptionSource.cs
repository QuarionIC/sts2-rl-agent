using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using Downfall.DownfallCode.Localization;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Localization;

public class FinisherDescriptionSource : IExtraDescriptionSource
{
	public IEnumerable<string> GetLines(CardModel card)
	{
		if (card.Tags.Contains(ChampTag.Finisher))
		{
			ChampStanceModel champStanceModel = ((((AbstractModel)card).IsCanonical || card._owner == null || card.CombatState == null) ? ChampModelDb.ChampStance<ChampNoStance>() : card.Owner.ChampStance());
			LocString val = new LocString("champ_stances", TypePrefix.GetPrefix(((object)champStanceModel).GetType()) + ((AbstractModel)champStanceModel).Id.Entry + ".finisher");
			champStanceModel.DynamicVars.AddTo(val);
			yield return val.GetFormattedText();
		}
	}
}
