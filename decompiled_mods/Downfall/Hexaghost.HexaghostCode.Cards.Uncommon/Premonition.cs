using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class Premonition : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public Premonition()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		CardType? cardType = await GetCardType(ctx);
		if (cardType.HasValue)
		{
			CardModel val = ((CardModel)this).CombatState.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetDraw((CardModel e) => (CardType?)e.Type == cardType));
			if (val != null)
			{
				await CardCmd.AutoPlay(ctx, val, (Creature)null, (AutoPlayType)1, false, false);
			}
		}
	}

	private async Task<CardType?> GetCardType(PlayerChoiceContext ctx)
	{
		List<PremonitionChoice> list = (from f in (from c in ((CardModel)this).Owner.GetDraw()
				select c.Type).Distinct()
			select PremonitionChoice.Create(f, ((CardModel)this).Owner)).ToList();
		int count = list.Count;
		if (count <= 1)
		{
			return count switch
			{
				0 => null, 
				1 => ((CardModel)list[0]).Type, 
				_ => null, 
			};
		}
		if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, false) is PremonitionChoice premonitionChoice)
		{
			return ((CardModel)premonitionChoice).Type;
		}
		return null;
	}
}
