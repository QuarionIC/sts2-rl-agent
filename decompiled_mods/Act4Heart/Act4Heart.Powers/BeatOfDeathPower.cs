using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Act4Heart.Powers;

internal class BeatOfDeathPower : A4hPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => false;

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		((PowerModel)this).Flash();
		NDebugAudioManager instance = NDebugAudioManager.Instance;
		if (instance != null)
		{
			instance.Play("SOTE_SFX_FastBlunt_v2.mp3", 0.4f, (PitchVariance)0);
		}
		return CreatureCmd.Damage(context, cardPlay.Card.Owner.Creature, (decimal)((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner);
	}
}
