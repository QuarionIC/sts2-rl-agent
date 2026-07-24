using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class RevengeProtocol : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public RevengeProtocol()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<BracingPower>(4, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<RevengeProtocolPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Brace));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<RevengeProtocolPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<BracingPower>(ctx, (CardModel)(object)this, false);
	}
}
