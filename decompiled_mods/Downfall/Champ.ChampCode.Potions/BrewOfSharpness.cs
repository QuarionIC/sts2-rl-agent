using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Potions;

[Pool(typeof(ChampPotionPool))]
public class BrewOfSharpness : ChampPotionModel
{
	public BrewOfSharpness()
		: base((PotionRarity)1, (PotionUsage)1, (TargetType)1)
	{
		WithPower<CounterPower>(25, showTip: true);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return MyCommonActions.ApplySelf<CounterPower>(ctx, (AbstractModel)(object)this);
	}
}
