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

public class Tonic : HermitPotionModel
{
	public Tonic()
		: base((PotionRarity)2, (PotionUsage)1, (TargetType)1)
	{
		WithPower<RuggedPower>(1, showTip: true);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return MyCommonActions.ApplySelf<RuggedPower>(ctx, (AbstractModel)(object)this);
	}
}
