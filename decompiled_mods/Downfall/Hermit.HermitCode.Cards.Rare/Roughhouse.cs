using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class Roughhouse : HermitCardModel, IHasDeadOnEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Roughhouse()
		: base(3, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(24, 6);
		((ConstructedCardModel)this).WithBlock(20, 4);
	}

	public async Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay play)
	{
		await CommonActions.CardBlock((CardModel)(object)this, play);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitBluntHeavyHitFx().Execute(ctx);
	}
}
