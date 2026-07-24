using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Downfall.DownfallCode.Abstract;

public interface IInstancedPerTarget
{
	Creature? TargetCreature { get; }
}
