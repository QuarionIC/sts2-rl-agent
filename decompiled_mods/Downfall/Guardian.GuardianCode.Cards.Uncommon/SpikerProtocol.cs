using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class SpikerProtocol : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Ez>();

	public SpikerProtocol()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<SpikerProtocolPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithBrace(6, 3);
		((ConstructedCardModel)(object)this).WithTip<ThornsPower>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<SpikerProtocolPower>(ctx, (CardModel)(object)this, false);
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
