using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class FrostPrimer : AutomatonRelicModel, IModifyCompiledFunction
{
	public FrostPrimer()
		: base((RelicRarity)4)
	{
		WithTip<Steady>();
	}

	public bool ModifyCompiledFunction(FunctionCard function, Player player)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return false;
		}
		CardCmd.Enchant<Steady>((CardModel)(object)function, 1m);
		return true;
	}

	public Task AfterModifyCompiledFunction(FunctionCard result, Player player)
	{
		return Task.CompletedTask;
	}
}
