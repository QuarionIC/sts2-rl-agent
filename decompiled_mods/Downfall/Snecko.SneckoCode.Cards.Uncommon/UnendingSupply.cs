using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class UnendingSupply : SneckoCardModel
{
	public UnendingSupply()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(DownfallKeyword.Echo));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)2));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)(object)this).WithPower<UnendingSupplyPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<UnendingSupplyPower>(ctx, (CardModel)(object)this, false);
	}
}
