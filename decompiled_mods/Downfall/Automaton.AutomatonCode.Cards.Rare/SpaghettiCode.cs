using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class SpaghettiCode : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public SpaghettiCode()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Rng rng = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection;
		List<CardModel> cards = CardFactory.FilterForCombat(from c in ((CardModel)this).Owner.Character.CardPool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where AutomatonCmd.IsEncodable(c) && (int)c.Rarity != 7
			select c).ToList();
		FunctionCard functionCard = null;
		while (functionCard == null)
		{
			List<CardModel> list = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, (IEnumerable<CardModel>)cards, 3, rng).ToList();
			CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, false);
			if (val == null)
			{
				break;
			}
			functionCard = await AutomatonCmd.EncodeCard(val, ctx);
		}
		FunctionCard functionCard2 = functionCard;
		if (functionCard2 != null)
		{
			((CardModel)functionCard2).SetToFreeThisTurn();
		}
	}
}
