using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Basic;

[Pool(typeof(AwakenedCardPool))]
public sealed class StrikeAwakened : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public StrikeAwakened()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, "vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
	}
}
