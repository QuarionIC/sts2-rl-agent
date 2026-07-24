using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Powers;

public class TyphoonFangPower : SneckoPowerModel, IAfterOverflowEffect
{
	private class CardDynamicVar : DynamicVar
	{
		public CardDynamicVar()
			: base("card", 0m)
		{
		}

		public override string ToString()
		{
			if (!(base._owner is TyphoonFangPower typhoonFangPower))
			{
				return "";
			}
			CardModel? dupe = typhoonFangPower.Dupe;
			return ((dupe != null) ? dupe.Title : null) ?? "";
		}
	}

	private CardPlay? _pendingCardPlay;

	private bool _shouldTrigger;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	private CardModel? Dupe { get; set; }

	private CardModel? Source { get; set; }

	public TyphoonFangPower()
		: base((PowerType)1, (PowerStackType)2)
	{
		WithVars(new CardDynamicVar());
		WithTips((PowerModel power) => (!(power is TyphoonFangPower { Dupe: not null } typhoonFangPower)) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)new CardHoverTip(typhoonFangPower.Dupe))));
	}

	public async Task AfterOverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay, CardModel card)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && Source != cardPlay.Card && Source != card && Dupe != null && !cardPlay.IsAutoPlay)
		{
			Creature val = ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
			CardModel val2 = (Dupe = Source?.CreateDupeCompat());
			if (val != null && val2 != null && LocalContext.NetId.HasValue)
			{
				await CardCmd.AutoPlay(ctx, val2, val, (AutoPlayType)1, false, false);
			}
		}
	}

	public override Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (!_shouldTrigger || _pendingCardPlay != cardPlay || Dupe == null)
		{
			return Task.CompletedTask;
		}
		_shouldTrigger = false;
		_pendingCardPlay = null;
		return Task.CompletedTask;
	}

	public void SetCard(CardModel card)
	{
		Dupe = card.CreateDupeCompat();
		Source = card;
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
