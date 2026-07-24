using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class RoundhouseKick : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public RoundhouseKick()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(13, 5);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithPower<RoundhouseKickPower>(10, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitBluntHeavyHitFx().Execute(ctx);
		await CommonActions.Apply<RoundhouseKickPower>(ctx, (CardModel)(object)this, play, false);
	}
}
