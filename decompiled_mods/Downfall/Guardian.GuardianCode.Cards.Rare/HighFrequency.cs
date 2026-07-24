using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class HighFrequency : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public override bool CanBeGeneratedInCombat => false;

	public HighFrequency()
		: base(3, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel card = (await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.StasisSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (card != null)
		{
			while (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner, null, silent: true))
			{
				CardModel a = card.CreateClone();
				await CardPileCmd.Add(a, (PileType)5, (CardPilePosition)1, (AbstractModel)null, false);
				await GuardianCmd.PutIntoStasis(a, ctx, (AbstractModel)(object)this, silent: true);
			}
			await CardCmd.Exhaust(ctx, card, false, false);
		}
	}
}
