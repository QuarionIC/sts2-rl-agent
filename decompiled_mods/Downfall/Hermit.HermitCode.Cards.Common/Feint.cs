using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Common;

public class Feint : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Feint()
		: base(0, (CardType)2, (CardRarity)2, (TargetType)3)
	{
		((ConstructedCardModel)this).WithBlock(3, 2);
		((ConstructedCardModel)this).WithPower<BruisePower>(2, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.CardBlock((CardModel)(object)this, play);
		await CommonActions.Apply<BruisePower>(ctx, (CardModel)(object)this, play, false);
	}
}
