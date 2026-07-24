using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class GuardianWhirl : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	protected override bool ShouldGlowGoldInternal => ((CardModel)this).Owner.Creature.Block >= ((CardModel)this).DynamicVars["Threshold"].IntValue;

	public GuardianWhirl()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)this).WithVar("Threshold", 16, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, (((CardModel)this).Owner.Creature.Block >= ((CardModel)this).DynamicVars["Threshold"].IntValue) ? 4 : 2, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
