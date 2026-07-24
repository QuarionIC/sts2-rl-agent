using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Champ.ChampCode.Stance;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Potions;

[Pool(typeof(ChampPotionPool))]
public class Stimpack : ChampPotionModel
{
	public Stimpack()
		: base((PotionRarity)3, (PotionUsage)1, (TargetType)1)
	{
		WithTips((PotionModel e) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(ChampModelDb.ChampStance<ChampUltimateStance>().HoverTip));
		WithPower<UltimateStancePower>(1, showTip: false);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return MyCommonActions.ApplySelf<UltimateStancePower>(ctx, (AbstractModel)(object)this);
	}
}
