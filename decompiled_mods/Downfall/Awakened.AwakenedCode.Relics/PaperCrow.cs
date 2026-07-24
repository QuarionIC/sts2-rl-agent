using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class PaperCrow : AwakenedRelicModel, IModifyManaburnDamage
{
	public PaperCrow()
		: base((RelicRarity)3)
	{
		WithTip<ManaburnPower>();
	}

	public decimal ModifyManaburnDamage(decimal amount, decimal original, Player player)
	{
		if (((RelicModel)this).Owner != player)
		{
			return amount;
		}
		return amount + original * 0.25m;
	}

	public Task AfterModifyingManaburnDamage(PlayerChoiceContext ctx, Player player)
	{
		((RelicModel)this).Flash();
		return Task.CompletedTask;
	}
}
