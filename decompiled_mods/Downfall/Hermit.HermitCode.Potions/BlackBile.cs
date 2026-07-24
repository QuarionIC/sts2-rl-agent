using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Potions;

public class BlackBile : HermitPotionModel
{
	public BlackBile()
		: base((PotionRarity)1, (PotionUsage)1, (TargetType)2)
	{
		WithPower<BruisePower>(6, showTip: true);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return MyCommonActions.Apply<BruisePower>(ctx, (AbstractModel)(object)this, target);
	}
}
