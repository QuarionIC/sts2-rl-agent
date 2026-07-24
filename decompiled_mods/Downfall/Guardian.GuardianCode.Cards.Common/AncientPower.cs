using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class AncientPower : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	public int GemSlots => 1;

	public AncientPower()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<AncientStrengthPower>(3, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<AncientDexterityPower>(3, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithTip<DexterityPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<AncientStrengthPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<AncientDexterityPower>(ctx, (CardModel)(object)this, false);
	}
}
