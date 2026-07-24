using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Basic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class BronzeCore : AutomatonRelicModel
{
	public BronzeCore()
		: base((RelicRarity)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		WithTip<StrikeAutomaton>();
		WithTip<DefendAutomaton>();
		WithTip(AutomatonTip.Encode);
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<PlatinumCore>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				((RelicModel)this).Flash();
				CardModel card = player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<DefendAutomaton>(), player);
				CardModel card2 = player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<StrikeAutomaton>(), player);
				await AutomatonCmd.EncodeCard(card, ctx);
				await AutomatonCmd.EncodeCard(card2, ctx);
			}
		}
	}
}
