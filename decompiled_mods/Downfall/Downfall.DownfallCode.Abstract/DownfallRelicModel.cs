using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallRelicModel<T> : ConstructedRelicModel where T : DownfallCharacterModel
{
	private string IconName => StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant();

	public override string PackedIconPath => (IconName + ".tres").TresRelicImagePath<T>();

	protected override string PackedIconOutlinePath => (IconName + "_outline.tres").TresRelicImagePath<T>();

	protected override string BigIconPath => (IconName + ".png").BigRelicImagePath<T>();

	protected DownfallRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
