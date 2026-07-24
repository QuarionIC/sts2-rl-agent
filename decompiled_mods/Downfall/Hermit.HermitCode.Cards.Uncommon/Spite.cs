using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Spite : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Spite()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(8, 2);
		((ConstructedCardModel)this).WithCards(3, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)4));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await (from c in ((CardModel)this).Owner.GetHand()
			where c.Keywords.Contains((CardKeyword)4)
			select c).ForEachAsync((CardModel card) => CardCmd.Exhaust(ctx, card, false, false));
		await CommonActions.CardBlock((CardModel)(object)this, play);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
