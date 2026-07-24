using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class Clone : GuardianCardModel
{
	public Clone()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithAccelerate(0, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = (await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.ApplySelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			CardModel clone = val.CreateClone();
			await CardPileCmd.AddGeneratedCardToCombat(clone, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
			await GuardianCmd.PutIntoStasis(clone, ctx, (AbstractModel)(object)this);
			if (((CardModel)this).IsUpgraded)
			{
				await GuardianCmd.Accelerate(ctx, (AbstractModel)(object)this);
			}
		}
	}
}
