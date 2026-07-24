using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Extensions;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class FindAndReplace : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public FindAndReplace()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithTip<Error>();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			List<CardModel> list = ((CardModel)this).Owner.GetStash().Concat(((CardModel)this).Owner.GetDraw()).Concat(((CardModel)this).Owner.GetDiscard())
				.ToList();
			CardSelectorPrefs val = default(CardSelectorPrefs);
			((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.ToHandSelectionPrompt, 1, 1);
			PileType[] array = (from e in list
				where e.Pile != null
				select e.Pile.Type).Distinct().ToArray();
			CardModel val2 = (await MultiPileCardSelect.Select(ctx, ((CardModel)this).Owner, val, list, array)).FirstOrDefault();
			CardPile sourcePile = ((val2 != null) ? val2.Pile : null);
			if (sourcePile != null && val2 != null)
			{
				int index = sourcePile._cards.IndexOf(val2);
				await CardPileCmd.Add(val2, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
				await DownfallCardCmd.AddWithIndex((CardModel)(object)((CardModel)this).CombatState.CreateCard<Error>(((CardModel)this).Owner), sourcePile, index);
			}
		}
	}
}
