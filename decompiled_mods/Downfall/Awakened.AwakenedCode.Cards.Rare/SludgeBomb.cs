using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class SludgeBomb : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool IsPlayable => ((CardModel)this).Owner.GetExhaust().Any((CardModel c) => c is Void);

	public SludgeBomb()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)(object)this).WithTip<Void>();
		((ConstructedCardModel)this).WithDamage(18, 4);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
