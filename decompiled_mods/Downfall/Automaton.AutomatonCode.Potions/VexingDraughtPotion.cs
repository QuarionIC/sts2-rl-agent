using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Potions;

[Pool(typeof(AutomatonPotionPool))]
public class VexingDraughtPotion : AutomatonPotionModel
{
	public VexingDraughtPotion()
		: base((PotionRarity)1, (PotionUsage)1, (TargetType)1)
	{
		WithPower<StrengthPower>(2, showTip: true);
		WithPower<DexterityPower>(2, showTip: true);
		((ConstructedPotionModel)this).WithTip<Burn>();
	}

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		await MyCommonActions.ApplySelf<StrengthPower>(ctx, (AbstractModel)(object)this);
		await MyCommonActions.ApplySelf<DexterityPower>(ctx, (AbstractModel)(object)this);
		await StashCmd.Stash<Burn>(((PotionModel)this).Owner, 2);
	}
}
