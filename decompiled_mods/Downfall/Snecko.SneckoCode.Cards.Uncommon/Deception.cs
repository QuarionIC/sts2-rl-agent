using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class Deception : SneckoCardModel
{
	public Deception()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(8, 3);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithTip<WeakPower>();
		((ConstructedCardModel)(object)this).WithTip<VulnerablePower>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithCalculatedVar("CalculatedPower", 1, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => ((CombatHistoryEntry)e).Actor == card.Owner.Creature && ((CombatHistoryEntry)e).HappenedThisTurn(card.CombatState));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (cardPlay.Target != null)
		{
			decimal amount = ((CalculatedVar)((CardModel)this).DynamicVars["CalculatedPower"]).Calculate(cardPlay.Target);
			await PowerCmd.Apply<WeakPower>(ctx, cardPlay.Target, amount, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			await PowerCmd.Apply<VulnerablePower>(ctx, cardPlay.Target, amount, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}
	}
}
