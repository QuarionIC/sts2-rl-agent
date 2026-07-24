using BaseLib.Abstracts;
using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallEnchantmentModel<T> : CustomEnchantmentModel where T : DownfallCharacterModel
{
	protected override string CustomIconPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".png").EnchantmentPath<T>();
}
