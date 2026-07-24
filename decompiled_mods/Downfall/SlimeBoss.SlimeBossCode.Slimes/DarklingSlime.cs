using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace SlimeBoss.SlimeBossCode.Slimes;

[Obsolete]
public class DarklingSlime : SlimeModel
{
	public override SlimeType SlimeType => SlimeType.None;

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return CustomMonsterModel.SetupAnimationState(controller, "Idle", (string)null, false, "Hit", false, "Attack", false, (string)null, false);
	}

	public override Task Command(PlayerChoiceContext ctx)
	{
		throw new Exception();
	}
}
