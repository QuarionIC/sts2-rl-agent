using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class PilotsCodex : GuardianRelicModel
{
	public PilotsCodex()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.Stasis);
	}

	public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return;
		}
		PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
		if (playerCombatState == null || playerCombatState.TurnNumber != 1)
		{
			return;
		}
		IEnumerable<CardModel> unlockedCards = ((CardPoolModel)ModelDb.CardPool<GuardianCardPool>()).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint);
		List<CardModel> list = CardFactory.GetDistinctForCombat(((RelicModel)this).Owner, unlockedCards, 2, ((RelicModel)this).Owner.RunState.Rng.CombatCardGeneration).ToList();
		((RelicModel)this).Flash();
		foreach (CardModel card in list)
		{
			await CardPileCmd.Add(card, (PileType)5, (CardPilePosition)1, (AbstractModel)null, false);
			await GuardianCmd.PutIntoStasis(card, ctx, (AbstractModel)(object)this, silent: true);
		}
	}
}
