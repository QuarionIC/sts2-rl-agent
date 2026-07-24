using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class Chomp : SlimeBossCardModel
{
	public Chomp()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel val = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetHand((CardModel e) => e.Tags.Contains(SlimeBossTag.Tackle)));
		if (val != null)
		{
			if (((CardModel)this).IsUpgraded)
			{
				val.SetToFreeThisCombat();
			}
			else
			{
				val.SetToFreeThisTurn();
			}
		}
	}
}
