using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class ArenaMastery : ChampCardModel
{
	public ArenaMastery()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<ArenaMasteryBerserkerPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<ArenaMasteryDefensivePower>(3, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithBerserkerTip();
		((ConstructedCardModel)(object)this).WithDefensiveTip();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Finisher));
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<ArenaMasteryBerserkerPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<ArenaMasteryDefensivePower>(ctx, (CardModel)(object)this, false);
	}
}
