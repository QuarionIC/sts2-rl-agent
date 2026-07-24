using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Psalm : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Psalm()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(10, 2);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
			await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		}
	}
}
