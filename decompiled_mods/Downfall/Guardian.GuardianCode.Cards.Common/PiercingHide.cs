using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class PiercingHide : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	public int GemSlots => 1;

	public PiercingHide()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 2);
		((ConstructedCardModel)(object)this).WithPower<PiercingHidePower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<ThornsPower>();
		((ConstructedCardModel)(object)this).WithBrace(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<PiercingHidePower>(ctx, (CardModel)(object)this, false);
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
