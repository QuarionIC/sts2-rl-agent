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

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class EvasiveProtocol : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public EvasiveProtocol()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithBrace(6, 3);
		((ConstructedCardModel)(object)this).WithPower<EvasiveProtocolPower>(1, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Polish));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<EvasiveProtocolPower>(ctx, (CardModel)(object)this, false);
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
