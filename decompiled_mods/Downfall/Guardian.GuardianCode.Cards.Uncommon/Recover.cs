using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class Recover : GuardianCardModel
{
	public Recover()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(5, 3);
		((ConstructedCardModel)(object)this).WithBrace(3, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner))
		{
			CardModel val = (await DownfallCardCmd.SelectFromCards(ctx, ((CardModel)this).Owner.GetDiscard(), DownfallCardSelectorPrefs.StasisSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
			if (val != null)
			{
				await GuardianCmd.PutIntoStasis(val, ctx, (AbstractModel)(object)this);
			}
		}
	}
}
