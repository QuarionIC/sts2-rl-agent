using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Multiplayer;

public class BigBruiser : HermitCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public BigBruiser()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)(object)this).WithPower<BigBruiserPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithPower<BruisePower>(3, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<BigBruiserPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.Apply<BruisePower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
