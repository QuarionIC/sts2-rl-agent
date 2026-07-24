using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class MakeshiftBlade : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public MakeshiftBlade()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			IsDebuff = true
		});
		((ConstructedCardModel)this).WithDamage(9, 4);
		((ConstructedCardModel)this).WithCards(3, 0);
		((ConstructedCardModel)this).WithVar("Debuffs", 3, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		Creature target = cardPlay.Target;
		if (((target != null) ? new int?(target.Powers.Count((PowerModel e) => e != null && (int)e.Type == 2 && e.Amount > 0)) : ((int?)null)) >= ((CardModel)this).DynamicVars["Debuffs"].IntValue)
		{
			await CommonActions.Draw((CardModel)(object)this, ctx);
		}
	}
}
