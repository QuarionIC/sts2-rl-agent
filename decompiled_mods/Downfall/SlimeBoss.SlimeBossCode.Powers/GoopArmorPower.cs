using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Events;

namespace SlimeBoss.SlimeBossCode.Powers;

public class GoopArmorPower : SlimeBossPowerModel, IAfterConsumeEffect
{
	public GoopArmorPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		WithTip((StaticHoverTip)5);
		WithTip(SlimeBossTip.Consume);
	}

	public Task AfterConsumeEffect(PlayerChoiceContext ctx, Creature creature, Creature attacker, decimal amount)
	{
		if (attacker == ((PowerModel)this).Owner)
		{
			return CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
		}
		return Task.CompletedTask;
	}
}
