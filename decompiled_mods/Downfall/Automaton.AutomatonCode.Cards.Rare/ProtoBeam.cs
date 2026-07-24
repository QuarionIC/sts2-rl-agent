using System;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class ProtoBeam : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ProtoBeam()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithCalculatedVar("CalculatedHits", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return card.Owner.GetExhaust().Count((CardModel e) => (int)e.Type == 4);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (int)((CalculatedVar)((CardModel)this).DynamicVars["CalculatedHits"]).Calculate((Creature)null);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
