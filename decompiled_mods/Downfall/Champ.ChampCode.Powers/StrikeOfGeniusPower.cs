using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class StrikeOfGeniusPower : ChampPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return;
		}
		IEnumerable<CardModel> enumerable = from e in player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
			where e.Tags.Contains((CardTag)1) && (int)e.Type == 1
			select e;
		List<CardModel> list = CardFactory.GetDistinctForCombat(player, enumerable, ((PowerModel)this).Amount, player.RunState.Rng.CombatCardGeneration).ToList();
		foreach (CardModel item in list)
		{
			item.EnergyCost.SetUntilPlayed(0, false);
			item.ToEcho();
		}
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)2, ((PowerModel)this).Owner.Player, (CardPilePosition)1);
	}

	public StrikeOfGeniusPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
