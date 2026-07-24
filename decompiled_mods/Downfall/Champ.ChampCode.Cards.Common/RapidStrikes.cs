using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class RapidStrikes : ChampCardModel
{
	public RapidStrikes()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithEnergyTip();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 2, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel obj = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>(PileTypeExtensions.GetPile((PileType)2, ((CardModel)this).Owner).Cards.Where((CardModel c) => c.Tags.Contains((CardTag)1) && c.EnergyCost.GetResolved() > 0 && !c.EnergyCost.CostsX));
		if (obj != null)
		{
			obj.EnergyCost.SetThisTurn(0, false);
		}
	}
}
