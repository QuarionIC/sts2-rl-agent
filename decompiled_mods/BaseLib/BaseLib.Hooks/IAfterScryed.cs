using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Hooks;

public interface IAfterScryed
{
	Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded);
}
