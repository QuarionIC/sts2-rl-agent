using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallAfflictionModel<T> : CustomAfflictionModel where T : DownfallCharacterModel
{
	public override string CustomOverlayPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".tscn").AfflictionScenePath<T>();
}
