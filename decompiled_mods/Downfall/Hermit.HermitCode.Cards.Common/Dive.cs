using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Common;

public sealed class Dive : HermitCardModel, IHasDeadOnEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Dive()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(8, 2);
		((ConstructedCardModel)this).WithPower<PlatedArmorPower>(1, 1);
	}

	public async Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay play)
	{
		await CommonActions.ApplySelf<PlatedArmorPower>(ctx, (CardModel)(object)this, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.CardBlock((CardModel)(object)this, play);
	}
}
