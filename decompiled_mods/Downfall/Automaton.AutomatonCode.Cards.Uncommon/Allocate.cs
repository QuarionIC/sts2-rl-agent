using System;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Extensions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Allocate : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Allocate()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)this).WithCalculatedVar("Status", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
	}

	private static decimal Calc(CardModel card, Creature? _)
	{
		return card.Owner.GetDraw().Count((CardModel c) => (int)c.Type == 4) + card.Owner.GetStash().Count((CardModel c) => (int)c.Type == 4);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(((CalculatedVar)((CardModel)this).DynamicVars["Status"]).Calculate((Creature)null), ((CardModel)this).Owner);
	}
}
