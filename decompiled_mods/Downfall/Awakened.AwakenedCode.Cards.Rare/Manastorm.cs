using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Extensions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Manastorm : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Manastorm()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(14, 4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AwakenedTip.Conjure));
		((ConstructedCardModel)(object)this).WithConjure();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
	}
}
