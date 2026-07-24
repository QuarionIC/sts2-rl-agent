using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallPotionModel : ConstructedPotionModel
{
	protected string IconName => StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant();

	protected virtual Artist? Artist => null;

	public override string CustomPackedImagePath => (IconName + ".tres").DownfallTresPotionImagePath();

	public override string CustomPackedOutlinePath => (IconName + "_outline.tres").DownfallTresPotionImagePath();

	protected DownfallPotionModel(PotionRarity potionRarity, PotionUsage potionUsage, TargetType targetType)
		: base(potionRarity, potionUsage, targetType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		WithTips((PotionModel e) => (!(e is DownfallPotionModel { Artist: not null } downfallPotionModel)) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(downfallPotionModel.Artist.HoverTip)));
	}
}
public abstract class DownfallPotionModel<T> : DownfallPotionModel where T : DownfallCharacterModel
{
	public override string CustomPackedImagePath => (base.IconName + ".tres").TresPotionImagePath<T>();

	public override string CustomPackedOutlinePath => (base.IconName + "_outline.tres").TresPotionImagePath<T>();

	protected DownfallPotionModel(PotionRarity potionRarity, PotionUsage potionUsage, TargetType targetType)
		: base(potionRarity, potionUsage, targetType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)

}
