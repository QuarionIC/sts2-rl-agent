using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class CustomAfflictionModel : AfflictionModel, ICustomModel
{
	public virtual string? CustomOverlayPath => null;
}
