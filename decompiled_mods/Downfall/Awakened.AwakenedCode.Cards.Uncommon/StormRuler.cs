using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class StormRuler : AwakenedCardModel
{
	public StormRuler()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<StormRulerPower>(6, 3, showTooltip: false);
		((ConstructedCardModel)(object)this).WithConjure();
		((ConstructedCardModel)(object)this).WithTip<Thunderbolt>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			await AwakenedCmd.Conjure(((CardModel)this).Owner);
			await CommonActions.ApplySelf<StormRulerPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
