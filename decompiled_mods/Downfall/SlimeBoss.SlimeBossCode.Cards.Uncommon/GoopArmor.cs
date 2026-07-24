using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class GoopArmor : SlimeBossCardModel
{
	public GoopArmor()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<GoopArmorPower>(3, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<GoopArmorPower>(ctx, (CardModel)(object)this, false);
	}
}
