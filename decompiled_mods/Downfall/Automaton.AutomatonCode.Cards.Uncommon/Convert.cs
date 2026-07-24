using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Extensions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Convert : AutomatonCardModel
{
	protected override bool ShouldGlowGoldInternal => ((CardModel)this).Owner.GetStash((CardModel e) => (int)e.Type == 4).Any();

	public Convert()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(9, 1);
		((ConstructedCardModel)this).WithUpgradingCardTip<Fuel>((Action<Fuel, CardModel>)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel val = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetStash((CardModel e) => (int)e.Type == 4));
		object obj;
		if (val == null)
		{
			obj = null;
		}
		else
		{
			ICardScope cardScope = val.CardScope;
			obj = ((cardScope != null) ? cardScope.CreateCard<Fuel>(val.Owner) : null);
		}
		Fuel val2 = (Fuel)obj;
		if (val2 != null && val != null)
		{
			if (((CardModel)this).IsUpgraded)
			{
				((CardModel)val2).UpgradeInternal();
			}
			await CardCmd.Transform(val, (CardModel)(object)val2, (CardPreviewStyle)1);
		}
	}
}
