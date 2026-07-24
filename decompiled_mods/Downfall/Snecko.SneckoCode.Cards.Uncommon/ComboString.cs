using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class ComboString : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public ComboString()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)3
		});
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithCalculatedVar("Repeat", 0, (Func<CardModel, Creature, decimal>)CalcDamage, 0, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	private static decimal CalcDamage(CardModel card, Creature? _)
	{
		return CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(card.CombatState) && SneckoCmd.IsOffclass(e.CardPlay.Card) && ((CombatHistoryEntry)e).Actor == card.Owner.Creature);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (int)((CardModel)this).DynamicVars["Repeat"].Calculate(null);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
