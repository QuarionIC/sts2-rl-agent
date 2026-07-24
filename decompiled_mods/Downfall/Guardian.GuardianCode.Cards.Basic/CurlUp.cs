using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Basic;

[Pool(typeof(GuardianCardPool))]
public class CurlUp : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public CurlUp()
		: base(1, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithBrace(10, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner))
		{
			CardModel val = (CardModel)((!((CardModel)this).IsUpgraded) ? ((object)((CardModel)this).CombatState.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetHand((CardModel e) => (object)e != this))) : ((object)(await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.StasisSelectionPrompt, (CardModel)(object)this)).FirstOrDefault()));
			if (val != null)
			{
				await GuardianCmd.PutIntoStasis(val, ctx, (AbstractModel)(object)this);
			}
		}
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
