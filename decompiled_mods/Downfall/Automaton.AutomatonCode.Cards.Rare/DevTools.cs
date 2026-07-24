using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Extensions;
using Automaton.AutomatonCode.Vfx;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class DevTools : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public DevTools()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithCalculatedVar("Dev", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return card.Owner.GetEncode().Count;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		decimal count = ((CalculatedVar)((CardModel)this).DynamicVars["Dev"]).Calculate((Creature)null);
		List<CardModel> list = ((CardModel)this).Owner.GetEncode().ToList();
		foreach (CardModel item in list)
		{
			await CardCmd.Exhaust(ctx, item, false, false);
		}
		await PlayerCmd.GainEnergy(count, ((CardModel)this).Owner);
		await CardPileCmd.Draw(ctx, count, ((CardModel)this).Owner, false);
		NSequenceDisplay.Refresh(((CardModel)this).Owner);
	}
}
