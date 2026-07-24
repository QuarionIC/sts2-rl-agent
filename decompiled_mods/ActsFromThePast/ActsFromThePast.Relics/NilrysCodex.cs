using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class NilrysCodex : CustomRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((RelicModel)this).Owner && CombatManager.Instance != null && CombatManager.Instance.IsInProgress)
		{
			((RelicModel)this).Flash();
			List<CardModel> cardChoices = CardFactory.GetDistinctForCombat(((RelicModel)this).Owner, ((RelicModel)this).Owner.Character.CardPool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint), 3, ((RelicModel)this).Owner.RunState.Rng.CombatCardGeneration).ToList();
			CardModel selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, (IReadOnlyList<CardModel>)cardChoices, ((RelicModel)this).Owner, true);
			if (selectedCard != null)
			{
				CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(selectedCard, (PileType)1, ((RelicModel)this).Owner, (CardPilePosition)3), 1.2f, (CardPreviewStyle)1);
			}
		}
	}

	public NilrysCodex()
		: base(true)
	{
	}
}
