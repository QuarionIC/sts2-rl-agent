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

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Horror : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Horror()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithPower<BruisePower>(3, 2);
		((ConstructedCardModel)(object)this).WithPower<HorrorPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.Apply<BruisePower>(ctx, (CardModel)(object)this, play, false);
		await CommonActions.Apply<HorrorPower>(ctx, (CardModel)(object)this, play, false);
	}
}
