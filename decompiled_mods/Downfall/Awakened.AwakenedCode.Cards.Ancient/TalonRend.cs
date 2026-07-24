using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Ancient;

[Pool(typeof(AwakenedCardPool))]
public class TalonRend : AwakenedCardModel
{
	public TalonRend()
		: base(1, (CardType)1, (CardRarity)5, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 3);
		((ConstructedCardModel)(object)this).WithRepeat(2);
		((ConstructedCardModel)(object)this).WithConjure();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, "vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
	}
}
