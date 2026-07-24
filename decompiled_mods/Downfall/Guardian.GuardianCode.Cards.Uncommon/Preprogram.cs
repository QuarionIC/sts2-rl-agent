using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class Preprogram : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Claude27A>();

	public int GemSlots => 1;

	public Preprogram()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCards(5, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner))
		{
			if (!((CardModel)this).Owner.GetDraw().Any())
			{
				await CardPileCmd.Shuffle(ctx, ((CardModel)this).Owner);
			}
			List<CardModel> cards = ((CardModel)this).Owner.GetDraw().Take(((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue).ToList();
			CardModel val = (await DownfallCardCmd.SelectFromCards(ctx, cards, DownfallCardSelectorPrefs.StasisSelectionPrompt, 1, (CardModel)(object)this)).FirstOrDefault();
			if (val != null)
			{
				await GuardianCmd.PutIntoStasis(val, ctx, (AbstractModel)(object)this);
			}
		}
	}
}
