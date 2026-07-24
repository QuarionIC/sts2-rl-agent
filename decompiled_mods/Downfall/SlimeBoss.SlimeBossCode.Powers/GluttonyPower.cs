using System;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.History;

namespace SlimeBoss.SlimeBossCode.Powers;

public class GluttonyPower : SlimeBossPowerModel, IAfterConsumeEffect
{
	private int ConsumeThisTurn => CombatManager.Instance.History.Entries.OfType<ConsumeEntry>().Count((ConsumeEntry e) => ((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState));

	public GluttonyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedPowerModel)this).WithTip<Lick>();
		WithTip(SlimeBossTip.Consume);
	}

	public async Task AfterConsumeEffect(PlayerChoiceContext ctx, Creature creature, Creature attacker, decimal amount)
	{
		if (attacker == ((PowerModel)this).Owner && ((PowerModel)this).Owner.Player != null && ConsumeThisTurn <= ((PowerModel)this).Amount)
		{
			await DownfallCardCmd.GiveCards<Lick>(((PowerModel)this).Owner.Player, (PileType)2, 1m, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Lick>?)null, (Player?)null);
		}
	}
}
