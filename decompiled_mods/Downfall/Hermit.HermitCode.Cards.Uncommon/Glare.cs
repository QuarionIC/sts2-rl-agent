using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Glare : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Glare()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, play, false);
		await CommonActions.Apply<VulnerablePower>(ctx, (CardModel)(object)this, play, false);
	}
}
