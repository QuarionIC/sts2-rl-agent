using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace BaseLib.Hooks;

public interface IModifyScryAmount
{
	int ModifyScryAmount(Player player, int amount);

	Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
