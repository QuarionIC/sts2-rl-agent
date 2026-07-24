using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Automaton.AutomatonCode.Core;

public abstract class AutomatonRelicModel : DownfallRelicModel<Automaton>
{
	protected AutomatonRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
