using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Multiplayer;

[Pool(typeof(HexaghostCardPool))]
public class EerieExpedition : HexaghostCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public EerieExpedition()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)7)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Afterlife));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		List<CardModel> cards = ModelDb.AllCharacterCardPools.SelectMany((CardPoolModel e) => from c in e.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where c.Keywords.Contains(HexaghostKeyword.Afterlife)
			select c).ToList();
		foreach (Player player in ((CardModel)this).CombatState.Players)
		{
			CardModel val = CardFactory.GetDistinctForCombat(player, (IEnumerable<CardModel>)cards, 1, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
			if (val != null)
			{
				val.SetToFreeThisTurn();
				await CardPileCmd.AddGeneratedCardToCombat(val, (PileType)2, player, (CardPilePosition)1);
			}
		}
	}
}
