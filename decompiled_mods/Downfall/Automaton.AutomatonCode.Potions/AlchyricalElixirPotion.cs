using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Powers;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Potions;

[Pool(typeof(AutomatonPotionPool))]
public class AlchyricalElixirPotion : AutomatonPotionModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Chimedragon>();

	public AlchyricalElixirPotion()
		: base((PotionRarity)2, (PotionUsage)1, (TargetType)1)
	{
		WithPower<AlchyricalElixirPower>(1, showTip: false);
	}

	protected override Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		return MyCommonActions.ApplySelf<AlchyricalElixirPower>(ctx, (AbstractModel)(object)this);
	}
}
