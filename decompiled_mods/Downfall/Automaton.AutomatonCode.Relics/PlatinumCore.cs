using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Basic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class PlatinumCore : AutomatonRelicModel
{
	public PlatinumCore()
		: base((RelicRarity)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		WithTip<StrikeAutomaton>();
		WithTip<DefendAutomaton>();
		WithTip(AutomatonTip.Encode);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		((RelicModel)this).Flash();
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				CardModel card = player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<StrikeAutomaton>(), player);
				CardModel card2 = player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<DefendAutomaton>(), player);
				await AutomatonCmd.EncodeCard(card, ctx);
				await AutomatonCmd.EncodeCard(card2, ctx);
			}
			List<CardModel> list = (from c in ((RelicModel)this).Owner.Character.CardPool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where AutomatonCmd.IsEncodable(c) && (int)c.Rarity != 7
				select c).ToList();
			Rng combatCardSelection = ((RelicModel)this).Owner.RunState.Rng.CombatCardSelection;
			CardModel val = CardFactory.GetDistinctForCombat(((RelicModel)this).Owner, (IEnumerable<CardModel>)list, 1, combatCardSelection).FirstOrDefault();
			if (val != null)
			{
				await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
}
