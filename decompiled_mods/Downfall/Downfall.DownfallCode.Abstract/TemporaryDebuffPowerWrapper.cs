using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class TemporaryDebuffPowerWrapper<TModel, TPower> : CustomTemporaryPowerModelWrapper<TModel, TPower> where TModel : AbstractModel where TPower : PowerModel
{
	protected override bool InvertInternalPowerAmount => true;

	public override PowerType Type => (PowerType)2;
}
