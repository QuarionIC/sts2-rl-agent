using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Ancient;

public sealed class FatalDesire : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public FatalDesire()
		: base(1, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
		((ConstructedCardModel)(object)this).WithPower<FatalDesirePower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<MachineLearningPower>(2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<Injury>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<MachineLearningPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<FatalDesirePower>(ctx, (CardModel)(object)this, false);
	}
}
