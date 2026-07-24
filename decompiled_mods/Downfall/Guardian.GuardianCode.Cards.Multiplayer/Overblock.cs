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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Multiplayer;

[Pool(typeof(GuardianCardPool))]
public class Overblock : GuardianCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Overblock()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
		((ConstructedCardModel)(object)this).WithTip<ThornsPower>();
		((ConstructedCardModel)(object)this).WithPower<OverblockBlockPower>(16, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<OverblockThornsPower>(3, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<OverblockBlockPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<OverblockThornsPower>(ctx, (CardModel)(object)this, false);
	}
}
