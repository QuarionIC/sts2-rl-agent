using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class NoHoldsBarred : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public NoHoldsBarred()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(19, 4);
		((ConstructedCardModel)this).WithPower<BruisePower>(5, 1);
		((ConstructedCardModel)this).WithEnergy(1, 0);
		((ConstructedCardModel)(object)this).WithPower<DrainedPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitSlashHitFx().Execute(ctx);
		await CommonActions.Apply<BruisePower>(ctx, (CardModel)(object)this, play, false);
		await CommonActions.ApplySelf<DrainedPower>(ctx, (CardModel)(object)this, false);
	}
}
