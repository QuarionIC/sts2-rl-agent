using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Relics;

public sealed class StraightRazor : HermitRelicModel
{
	public StraightRazor()
		: base((RelicRarity)3)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		WithVars((DynamicVar)new HealVar(15m));
		WithTip((StaticHoverTip)4);
	}

	public override async Task BeforeCardRemoved(CardModel card)
	{
		if (card.Owner == ((RelicModel)this).Owner)
		{
			await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue, true);
		}
	}
}
