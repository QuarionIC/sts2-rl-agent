using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class UnleashSpirits : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public UnleashSpirits()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)4)
	{
		((ConstructedCardModel)this).WithDamage(10, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)this).WithCalculatedVar("Repeat", 1, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
	}

	private static decimal Calc(CardModel card, Creature? target)
	{
		ICombatState combatState = card.CombatState;
		if (combatState == null)
		{
			return 0m;
		}
		return CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count((CardExhaustedEntry e) => ((CombatHistoryEntry)e).RoundNumber == combatState.RoundNumber - 1 && ((CombatHistoryEntry)e).Actor == card.Owner.Creature);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		decimal num = ((CalculatedVar)(CustomCalculatedVar)((CardModel)this).DynamicVars["Repeat"]).Calculate((Creature)null);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, (int)num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
