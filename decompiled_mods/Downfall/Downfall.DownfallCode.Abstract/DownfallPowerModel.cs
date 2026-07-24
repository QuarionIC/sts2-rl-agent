using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallPowerModel : ConstructedPowerModel
{
	protected string IconName => StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant();

	public override string CustomPackedIconPath => (IconName + ".tres").DownfallPowerImagePath();

	public override string CustomBigIconPath => (IconName + ".png").DownfallBigPowerImagePath();

	public virtual string CustomPackedSpritePath => (IconName + ".tres").DownfallPowerSpriteImagePath();

	protected DownfallPowerModel(PowerType powerType = (PowerType)1, PowerStackType powerStackType = (PowerStackType)1)
		: base(powerType, powerStackType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)

}
public abstract class DownfallPowerModel<T> : DownfallPowerModel where T : DownfallCharacterModel
{
	public override string CustomPackedIconPath => (base.IconName + ".tres").PowerImagePath<T>();

	public override string CustomBigIconPath => (base.IconName + ".png").BigPowerImagePath<T>();

	public override string CustomPackedSpritePath => (base.IconName + ".tres").PowerSpriteImagePath<T>();

	protected DownfallPowerModel(PowerType powerType = (PowerType)1, PowerStackType powerStackType = (PowerStackType)1)
		: base(powerType, powerStackType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)

}
